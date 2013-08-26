using System.Management.Automation;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive.Messages;
using Sitecore.Jobs.AsyncUI;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive
{
    [Cmdlet(VerbsCommon.Close, "Window")]
    public class CloseWindowCommand : BaseFormCommand
    {
        protected override void ProcessRecord()
        {
            JobContext.MessageQueue.PutMessage(new CloseWindowMessage());
        }
    }
}