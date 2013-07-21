using System.Management.Automation;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive.Messages;
using Sitecore.Data.Items;
using Sitecore.Jobs.AsyncUI;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive
{
    [Cmdlet("Execute", "ShellCommand")]
    public class ExecuteShellCommandCommand : BaseShellCommand
    {
        /*[ValidatePattern(@"[\*\?\[\]\-0-9a-zA-Z_]+\:[\*\?\[\]\-0-9a-zA-Z_]+")]*/
        [Parameter(Position = 0, Mandatory = true)]
        public string Name { get; set; }

        [Parameter(ValueFromPipeline = true, Position = 1)]
        public Item Item { get; set; }

        protected override void ProcessRecord()
        {
            LogErrors(() =>
                {
                    if (Sitecore.Context.Job != null)
                    {
                        JobContext.MessageQueue.PutMessage(new ShellCommandInItemContextMessage(Item, Name));
                        if (Item != null)
                        {
                            WriteItem(Item);
                        }
                    }
                });
        }
    }
}