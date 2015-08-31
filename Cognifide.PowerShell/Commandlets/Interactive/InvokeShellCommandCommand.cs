using System.Management.Automation;
using Cognifide.PowerShell.Commandlets.Interactive.Messages;
using Sitecore;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Interactive
{
    [Cmdlet(VerbsLifecycle.Invoke, "ShellCommand")]
    [OutputType(typeof (Item))]
    public class InvokeShellCommandCommand : BaseItemCommand
    {
        [Parameter(Position = 0, Mandatory = true)]
        public string Name { get; set; }

        protected override void ProcessItem(Item item)
        {
            LogErrors(() =>
            {
                if (CheckSessionCanDoInteractiveAction())
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