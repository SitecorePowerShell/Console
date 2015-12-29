using System;

namespace Cognifide.PowerShell.Client.Applications
{
    public class RunnerOutput
    {
        public string Output { get; set; }
        public Exception Exception { get; set; }
        public bool HasErrors { get; set; }
        public bool CloseRunner { get; set; }
    }
}