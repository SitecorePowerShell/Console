using System;
using System.Collections.Generic;

namespace Spe.Client.Applications
{
    public class RunnerOutput
    {
        public string Output { get; set; }
        public string DialogResult { get; set; }
        public Exception Exception { get; set; }
        public bool HasErrors { get; set; }
        public bool CloseRunner { get; set; }
        public List<string> CloseMessages { get; set; }
        
    }
}