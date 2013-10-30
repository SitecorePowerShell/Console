using System.Collections.Generic;
using System.Text;

namespace Cognifide.PowerShell.PowerShellIntegrations.Host
{
    public class OutputBuffer : List<OutputLine>
    {
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
    }
}