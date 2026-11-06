using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Buckets.Extensions;
using Spe.Core.Settings.Authorization;

namespace Spe.Core.Host
{
    public class OutputBuffer : List<OutputLine>
    {
        private int updatePointer = 0;
        private bool clearPending = false;
        public bool HasErrors { get; set; }

        // Warning and error records written through the host UI since the
        // last ResetRunState(); the Runner reports them when a script ends.
        public int WarningCount { get; set; }
        public int ErrorCount { get; set; }
        public bool SilenceOutput { get; set; }
        public OutputBuffer SilencedOutput { get; private set; }

        /// <summary>
        /// Returns true and resets the flag if a Clear was requested since the
        /// last call. Used by client output pollers (ISE PrintOutput, Console
        /// PollCommandOutput) to tell the browser to purge its displayed output
        /// when Clear-Host is invoked from within a running script.
        /// </summary>
        public bool ConsumeClearPending()
        {
            if (!clearPending) return false;
            clearPending = false;
            return true;
        }

        /// <summary>
        /// Forgets the error/warning state of the previous script run, so a
        /// persistent session reports each run on its own.
        /// </summary>
        public void ResetRunState()
        {
            HasErrors = false;
            WarningCount = 0;
            ErrorCount = 0;
            errorRecordOpen = false;
        }

        private bool errorRecordOpen;

        /// <summary>
        /// Counts error records from the lines the host writes for them.
        /// Out-Default writes one ErrorRecord as several WriteErrorLine calls
        /// (message, position, CategoryInfo, FullyQualifiedErrorId, then a
        /// blank line), so a record starts at its first non-blank line and
        /// ends at the FullyQualifiedErrorId line or a blank line.
        /// </summary>
        public void CountErrorLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                errorRecordOpen = false;
                return;
            }

            if (!errorRecordOpen)
            {
                ErrorCount++;
                errorRecordOpen = true;
            }

            if (line.TrimStart().StartsWith("+ FullyQualifiedErrorId", StringComparison.Ordinal))
            {
                errorRecordOpen = false;
            }
        }

        public string ToHtml(bool enableGuidLink = true)
        {
            var output = new StringBuilder(10240);
            foreach (var outputLine in this)
            {
                outputLine.GetHtmlLine(output, enableGuidLink);
            }
            return output.ToString();
        }

        public string GetHtmlUpdate()
        {
            List<OutputLine> lines;
            lock (this)
            {
                lines = this.Skip(updatePointer).ToList();
                updatePointer += lines.Count();
            }
            var output = new StringBuilder(10240);
            foreach (var outputLine in lines)
            {
                outputLine.GetHtmlLine(output);
            }
            return output.ToString();
        }

        public bool HasUpdates()
        {
            return updatePointer < Count;
        }

        /// <summary>
        /// Records that a streaming consumer has sent the first
        /// <paramref name="lineCount"/> lines, so a later GetConsoleUpdate
        /// starts after them. Lines added since the consumer's snapshot, and an
        /// unterminated line it sent only as a partial, stay unsent.
        /// </summary>
        public void MarkSent(int lineCount)
        {
            lock (this)
            {
                updatePointer = Math.Min(lineCount, Count);
            }
        }

        /// <param name="includeUnterminated">
        /// Also emit a trailing line with no terminator yet (Write-Host
        /// -NoNewline). Only for the final drain after a script ended, when the
        /// line can no longer grow.
        /// </param>
        public bool GetConsoleUpdate(StringBuilder output, int maxBufferSize, bool includeUnterminated = false)
        {
            if (!HasUpdates())
            {
                return false;
            }
            var buffer = WebServiceSettings.SerializationSizeBuffer;
            var temp = new StringBuilder();

            List<OutputLine> lines;
            lock (this)
            {
                lines = this.Skip(updatePointer).ToList();
            }

            var unterminatedLines = 1;
            foreach (var outputLine in lines)
            {
                outputLine.GetLine(temp, OutputLine.FormatResponseJsterm);
                if ((output.Length + temp.Length + buffer) > maxBufferSize)
                {
                    break;
                }
                // only full lines can be sent to terminal;
                if (outputLine.Terminated)
                {
                    updatePointer += unterminatedLines;
                    output.Append(temp);
                    temp.Clear();
                    unterminatedLines = 1;
                }
                else
                {
                    unterminatedLines++;
                }
            }

            if (includeUnterminated && temp.Length > 0 && (output.Length + temp.Length + buffer) <= maxBufferSize)
            {
                updatePointer += unterminatedLines - 1;
                output.Append(temp);
            }
            return true;
        }


        public override string ToString()
        {
            var output = new StringBuilder(10240);
            foreach (var outputLine in this)
            {
                outputLine.GetPlainTextLine(output);
            }
            return output.ToString();
        }

        public string GetDialogRessult()
        {
            return this.AsEnumerable().Reverse().FirstOrDefault(x => !x.Text.IsNullOrEmpty())?.Text;
        }
        
        public string ToJsTerminalString()
        {
            var output = new StringBuilder(10240);
            foreach (var outputLine in this)
            {
                outputLine.GetTerminalLine(output);
            }
            return output.ToString();
        }

        public void AddRange(IEnumerable<OutputLine> collection, int maxHeight = int.MaxValue)
        {
            var lines = collection as ICollection<OutputLine> ?? collection.ToList();
            collection = lines;

            // Any other output ends an error record, so a record that never got
            // its closing lines (a raw $host.UI.WriteErrorLine) cannot swallow
            // the next error. See CountErrorLine.
            if (lines.Any(line => line.LineType != OutputLineType.Error))
            {
                errorRecordOpen = false;
            }

            if (SilenceOutput)
            {
                if (SilencedOutput == null)
                {
                    SilencedOutput = new OutputBuffer();
                }

                SilencedOutput.AddRange(collection);
            }
            else
            {
                lock (this)
                {
                    base.AddRange(collection);
                }
            }

            EnsureMaxHeight(maxHeight);
        }

        private void EnsureMaxHeight(int maxHeight)
        {
            if (maxHeight < Count)
            {
                var outCount = 0;
                foreach (var line in this)
                {
                    if (line.Terminated && maxHeight > 0)
                    {
                        outCount++;
                        maxHeight--;
                    }
                }

                if (outCount < Count)
                {
                    this.RemoveRange(0,Count - outCount);  
                }
            }
        }

        public new void Clear()
        {
            updatePointer = 0;
            clearPending = true;
            base.Clear();
            SilencedOutput?.Clear();
        }

        /// <summary>
        /// Clears the buffer without signaling the client to purge its terminal
        /// output. Used for post-execution / post-poll cleanup where the output
        /// has already been consumed and we're just freeing server-side memory -
        /// unlike Clear() (invoked by Clear-Host via SetBufferContents) which
        /// must propagate to the browser.
        /// </summary>
        public void ClearSilent()
        {
            updatePointer = 0;
            base.Clear();
            SilencedOutput?.ClearSilent();
        }
    }
}