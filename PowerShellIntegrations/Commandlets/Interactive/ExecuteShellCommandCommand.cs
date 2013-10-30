using System.Management.Automation;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive.Messages;
using Sitecore;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive
{
    [Cmdlet("Execute", "ShellCommand")]
    [OutputType(new[] {typeof (Item)})]
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
                if (Context.Job != null)
                {
                    PutMessage(new ShellCommandInItemContextMessage(Item, Name));
                    if (Item != null)
                    {
                        WriteItem(Item);
                    }
                }
            });
        }
    }
}