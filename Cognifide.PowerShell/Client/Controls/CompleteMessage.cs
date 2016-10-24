using Cognifide.PowerShell.Client.Applications;
using Sitecore.Jobs.AsyncUI;

namespace Cognifide.PowerShell.Client.Controls
{
    public class CompleteMessage : FlushMessage
    {
        public RunnerOutput RunnerOutput { get; set; }
    }
}