using System;
using Cognifide.PowerShell.Client.Applications;

namespace Cognifide.PowerShell.Client.Controls
{
    public class SessionCompleteEventArgs : EventArgs
    {
        public RunnerOutput RunnerOutput { get; set; }
    }
}