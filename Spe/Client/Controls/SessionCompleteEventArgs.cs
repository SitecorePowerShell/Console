using System;
using Spe.Client.Applications;

namespace Spe.Client.Controls
{
    public class SessionCompleteEventArgs : EventArgs
    {
        public RunnerOutput RunnerOutput { get; set; }
    }
}