using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cognifide.PowerShell.Core.Host
{
    public class OutputBuffer : List<OutputLine>
    {
        private int updatePointer = 0;
        public bool HasErrors { get; set; }

        public string ToHtml()
        {
            var output = new StringBuilder(10240);
            foreach (var outputLine in this)
            {
                outputLine.GetHtmlLine(output);
            }
            return output.ToString();
        }

        public string ToHtmlUpdate()
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
            lock (this)
            {
                base.AddRange(collection);                
            }
        }

        public new void Clear()
        {
            updatePointer = 0;
            base.Clear();
        }
    }
}