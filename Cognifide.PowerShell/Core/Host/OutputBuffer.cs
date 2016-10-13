using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cognifide.PowerShell.Core.Settings.Authorization;

namespace Cognifide.PowerShell.Core.Host
{
    public class OutputBuffer : List<OutputLine>
    {
        private int updatePointer = 0;
        public bool HasErrors { get; set; }
        public bool SilenceOutput { get; set; }
        public OutputBuffer SilencedOutput { get; private set; }

        public string ToHtml()
        {
            var output = new StringBuilder(10240);
            foreach (var outputLine in this)
            {
                outputLine.GetHtmlLine(output);
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

        public string ToJsTerminalString()
        {
            var output = new StringBuilder(10240);
            foreach (var outputLine in this)
            {
                outputLine.GetTerminalLine(output);
            }
            return output.ToString();
        }

        public new void AddRange(IEnumerable<OutputLine> collection)
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
        }

        public new void Clear()
        {
            updatePointer = 0;
            base.Clear();
            SilencedOutput?.Clear();
        }
    }
}