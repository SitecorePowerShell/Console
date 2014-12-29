using System.Management.Automation;
using Cognifide.PowerShell.Commandlets.Interactive.Messages;
using Sitecore;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Interactive
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