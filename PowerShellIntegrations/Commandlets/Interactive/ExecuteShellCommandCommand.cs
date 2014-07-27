using System.Management.Automation;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive.Messages;
using Sitecore;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive
{
    [Cmdlet("Execute", "ShellCommand")]
    [OutputType(new[] {typeof (Item)})]
    public class ExecuteShellCommandCommand : BaseItemCommand
    {
        [Parameter(Position = 0, Mandatory = true)]
        public string Name { get; set; }

        protected override void ProcessItem(Item item)
        {
            LogErrors(() =>
            {
                if (Context.Job != null)
                {
                    PutMessage(new ShellCommandInItemContextMessage(item, Name));
                    if (item != null)
                    {
                        WriteItem(item);
                    }
                }
            });
        }
    }
}