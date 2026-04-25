using System;
using System.Collections.Concurrent;
using System.Threading;
using Spe.Core.Diagnostics;

namespace Spe.Core.Settings.Authorization
{
    // In-memory replay protection for OAuth bearer tokens. Keyed by
    // "{iss}|{jti}", value is the token's exp unix seconds. Process-local:
    // a multi-node CM cluster can replay across sibling nodes until
    // distributed cache lands. Documented in Spe.OAuthBearer.config.example.
    internal sealed class JtiReplayCache
    {
        private const int SweepInterval = 1000;

        private readonly ConcurrentDictionary<string, long> _cache =
            new ConcurrentDictionary<string, long>(StringComparer.Ordinal);

        private long _operationCount;
        private long _lastWarnTicks;

        public int MaxEntries { get; }

        public JtiReplayCache(int maxEntries)
        {
            MaxEntries = maxEntries > 0 ? maxEntries : 10000;
        }

        // Returns true on first use of (iss, jti) within the token's exp
        // window. Returns false on replay or unusable input.
        public bool TryClaim(string iss, string jti, long expUnixSeconds)
        {
            if (string.IsNullOrEmpty(iss) || string.IsNullOrEmpty(jti)) return false;

            var key = iss + "|" + jti;
            var nowSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (Interlocked.Increment(ref _operationCount) % SweepInterval == 0)
            {
                SweepExpired(nowSeconds);
            }

            while (true)
            {
                if (_cache.TryAdd(key, expUnixSeconds))
                {
                    if (_cache.Count > MaxEntries)
                    {
                        SweepExpired(nowSeconds);
                        if (_cache.Count > MaxEntries) WarnSoftCap();
                    }
                    return true;
                }

                if (!_cache.TryGetValue(key, out var existingExp)) continue;

                if (existingExp > nowSeconds) return false;

                if (_cache.TryUpdate(key, expUnixSeconds, existingExp)) return true;
            }
        }

        private void SweepExpired(long nowSeconds)
        {
            foreach (var kvp in _cache)
            {
                if (kvp.Value <= nowSeconds)
                {
                    _cache.TryRemove(kvp.Key, out _);
                }
            }
        }

        // Throttled to once a minute so a sustained over-cap state does not flood the log.
        private void WarnSoftCap()
        {
            var nowTicks = DateTime.UtcNow.Ticks;
            var lastTicks = Interlocked.Read(ref _lastWarnTicks);
            if (nowTicks - lastTicks < TimeSpan.TicksPerMinute) return;
            if (Interlocked.CompareExchange(ref _lastWarnTicks, nowTicks, lastTicks) != lastTicks) return;

            PowerShellLog.Warn($"[OAuthBearer] action=jtiCacheSoftCap count={_cache.Count} max={MaxEntries}");
        }

        internal int Count => _cache.Count;

        internal void Clear() => _cache.Clear();
    }
}
