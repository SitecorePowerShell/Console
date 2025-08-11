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
        public bool HasErrors { get; set; }
        public bool SilenceOutput { get; set; }
        public OutputBuffer SilencedOutput { get; private set; }

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

        public bool GetConsoleUpdate(StringBuilder output, int maxBufferSize)
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

        public new void AddRange(IEnumerable<OutputLine> collection, int maxHeight = Int32.MaxValue)
        {
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
            base.Clear();
            SilencedOutput?.Clear();
        }
    }
}