using System;
using System.Collections.Generic;
using System.Linq;

namespace Spe.Core.Host
{
    /// <summary>
    /// A bounded per-session buffer that captures Verbose / Information / Progress / Warning / Error
    /// records emitted by a script's runspace, so the wait endpoint can stream them back
    /// to the caller during the long-poll loop.
    ///
    /// <para>Bounds are enforced to make the ring safe under script-authored flooding:
    /// per-record character cap (truncated), total approximate-bytes cap (drop-oldest),
    /// and rate cap (token bucket; over-rate writes are dropped). A monotonic sequence
    /// number is assigned to every record so cursored reads can resume cleanly.</para>
    /// </summary>
    internal sealed class StreamRecordRing
    {
        // Caps. Values chosen to keep total memory per session bounded under
        // adversarial scripts: 100 rec/s * 4 KB max = 400 KB/s sustained; the
        // total cap clamps at ~256 records' worth, drop-oldest beyond that.
        private const int PerRecordCharCap = 4 * 1024;
        private const int TotalByteCap     = 1024 * 1024;
        private const int RatePerSecond    = 100;
        private const int RateBurst        = 100;

        private readonly object _gate = new object();
        private readonly Queue<StreamRecord> _records = new Queue<StreamRecord>();
        private long _nextSequence;
        private long _droppedRate;
        private long _droppedSize;
        private long _truncatedCount;
        private int  _approxBytes;

        // Token bucket for rate limiting.
        private double _tokens = RateBurst;
        private DateTime _lastRefillUtc = DateTime.UtcNow;

        public void AddVerbose(string message)     => Add(StreamKinds.Verbose,     message);
        public void AddInformation(string message) => Add(StreamKinds.Information, message);
        public void AddWarning(string message)     => Add(StreamKinds.Warning,     message);

        public void AddProgress(string activity, string status, int percentComplete,
                                string currentOperation, int secondsRemaining,
                                int parentActivityId, string recordType)
        {
            // Track truncation across the multi-field payload so the counter
            // reflects records affected by truncation, not field-level events
            // (a record with three over-cap fields counts once, like a verbose
            // record over the cap counts once).
            bool truncated = false;
            var payload = new ProgressPayload
            {
                Activity         = TruncateTrack(activity,         ref truncated),
                Status           = TruncateTrack(status,           ref truncated),
                PercentComplete  = percentComplete,
                CurrentOperation = TruncateTrack(currentOperation, ref truncated),
                SecondsRemaining = secondsRemaining,
                ParentActivityId = parentActivityId,
                RecordType       = recordType
            };
            Add(StreamKinds.Progress, payload, truncated);
        }

        public void AddError(string message, string fullyQualifiedErrorId, string categoryInfo,
                             string positionMessage, string scriptStackTrace)
        {
            bool truncated = false;
            var payload = new ErrorPayload
            {
                Message               = TruncateTrack(message,               ref truncated),
                FullyQualifiedErrorId = TruncateTrack(fullyQualifiedErrorId, ref truncated),
                CategoryInfo          = TruncateTrack(categoryInfo,          ref truncated),
                PositionMessage       = TruncateTrack(positionMessage,       ref truncated),
                ScriptStackTrace      = TruncateTrack(scriptStackTrace,      ref truncated)
            };
            Add(StreamKinds.Error, payload, truncated);
        }

        private void Add(string stream, object payload, bool preTruncated = false)
        {
            lock (_gate)
            {
                if (!RefillAndConsume())
                {
                    _droppedRate++;
                    return;
                }

                bool truncatedHere;
                payload = MaybeTruncate(payload, out truncatedHere);
                if (preTruncated || truncatedHere) _truncatedCount++;
                var bytes = EstimateBytes(payload);

                while (_approxBytes + bytes > TotalByteCap && _records.Count > 0)
                {
                    var oldest = _records.Dequeue();
                    _approxBytes = Math.Max(0, _approxBytes - EstimateBytes(oldest.Payload));
                    _droppedSize++;
                }

                _records.Enqueue(new StreamRecord(stream, _nextSequence++, payload));
                _approxBytes += bytes;
            }
        }

        /// <summary>
        /// Read all records with sequence &gt;= <paramref name="fromSequence"/>, capped at
        /// <paramref name="maxRecords"/>. Returns the offset to pass next time, the running
        /// total of dropped records (so callers can detect gaps), and the producer's high
        /// watermark.
        /// </summary>
        public StreamSnapshot ReadFrom(long fromSequence, int maxRecords)
        {
            lock (_gate)
            {
                var available = _records.Where(r => r.Sequence >= fromSequence).Take(maxRecords).ToArray();
                long nextOffset;
                if (available.Length > 0)
                {
                    nextOffset = available[available.Length - 1].Sequence + 1;
                }
                else if (_records.Count > 0 && fromSequence < _records.Peek().Sequence)
                {
                    // Caller is behind the head of the ring (drop-oldest evicted records they hadn't read).
                    // Bring them up to the current high watermark; their droppedCount comparison will tell them.
                    nextOffset = _nextSequence;
                }
                else
                {
                    nextOffset = fromSequence;
                }

                return new StreamSnapshot(available, nextOffset, _droppedRate, _droppedSize, _truncatedCount, _nextSequence);
            }
        }

        private bool RefillAndConsume()
        {
            var now = DateTime.UtcNow;
            var elapsed = (now - _lastRefillUtc).TotalSeconds;
            if (elapsed > 0)
            {
                _tokens = Math.Min(RateBurst, _tokens + elapsed * RatePerSecond);
                _lastRefillUtc = now;
            }
            if (_tokens < 1.0) return false;
            _tokens -= 1.0;
            return true;
        }

        private static object MaybeTruncate(object payload, out bool truncated)
        {
            truncated = false;
            if (payload is string s)
            {
                var t = Truncate(s);
                truncated = !ReferenceEquals(t, s);
                return t;
            }
            return payload;
        }

        private static string Truncate(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (s.Length <= PerRecordCharCap) return s;
            return s.Substring(0, PerRecordCharCap - 14) + "[...truncated]";
        }

        private static string TruncateTrack(string s, ref bool any)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (s.Length <= PerRecordCharCap) return s;
            any = true;
            return s.Substring(0, PerRecordCharCap - 14) + "[...truncated]";
        }

        private static int EstimateBytes(object payload)
        {
            if (payload is string s) return s.Length * 2; // UTF-16 char ~= 2 bytes
            if (payload is ProgressPayload p)
            {
                return ((p.Activity?.Length ?? 0) +
                        (p.Status?.Length ?? 0) +
                        (p.CurrentOperation?.Length ?? 0) +
                        (p.RecordType?.Length ?? 0) + 32) * 2;
            }
            if (payload is ErrorPayload e)
            {
                return ((e.Message?.Length ?? 0) +
                        (e.FullyQualifiedErrorId?.Length ?? 0) +
                        (e.CategoryInfo?.Length ?? 0) +
                        (e.PositionMessage?.Length ?? 0) +
                        (e.ScriptStackTrace?.Length ?? 0) + 32) * 2;
            }
            return 256;
        }
    }

    internal static class StreamKinds
    {
        public const string Verbose     = "verbose";
        public const string Information = "information";
        public const string Progress    = "progress";
        public const string Warning     = "warning";
        public const string Error       = "error";
    }

    internal sealed class StreamRecord
    {
        public string   Stream    { get; }
        public long     Sequence  { get; }
        public DateTime TimeUtc   { get; }
        public object   Payload   { get; }

        public StreamRecord(string stream, long sequence, object payload)
        {
            Stream   = stream;
            Sequence = sequence;
            TimeUtc  = DateTime.UtcNow;
            Payload  = payload;
        }
    }

    internal sealed class ProgressPayload
    {
        public string Activity         { get; set; }
        public string Status           { get; set; }
        public int    PercentComplete  { get; set; }
        public string CurrentOperation { get; set; }
        public int    SecondsRemaining { get; set; }
        public int    ParentActivityId { get; set; }
        public string RecordType       { get; set; }
    }

    internal sealed class ErrorPayload
    {
        public string Message               { get; set; }
        public string FullyQualifiedErrorId { get; set; }
        public string CategoryInfo          { get; set; }
        public string PositionMessage       { get; set; }
        public string ScriptStackTrace      { get; set; }
    }

    internal sealed class StreamSnapshot
    {
        public IReadOnlyList<StreamRecord> Records         { get; }
        public long                        NextOffset      { get; }
        public long                        DroppedRate     { get; }
        public long                        DroppedSize     { get; }
        public long                        TruncatedCount  { get; }
        public long                        ProducedSoFar   { get; }

        public long TotalDropped => DroppedRate + DroppedSize;

        public StreamSnapshot(IReadOnlyList<StreamRecord> records, long nextOffset,
                              long droppedRate, long droppedSize, long truncatedCount, long producedSoFar)
        {
            Records        = records;
            NextOffset     = nextOffset;
            DroppedRate    = droppedRate;
            DroppedSize    = droppedSize;
            TruncatedCount = truncatedCount;
            ProducedSoFar  = producedSoFar;
        }
    }
}
