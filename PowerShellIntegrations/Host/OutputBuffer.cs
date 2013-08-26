using System.Collections.Generic;

namespace Cognifide.PowerShell.PowerShellIntegrations.Host
{
    public class OutputBuffer : List<OutputLine>
    {
        public bool HasErrors { get; set; }
    }
}