using System;
using System.Management.Automation;
using Cognifide.PowerShell.Shell.Commands.Interactive.Messages;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Shell.Framework.Commands;

namespace Cognifide.PowerShell.Shell.Commands.Interactive
{
    [Cmdlet("Execute", "ShellCommand", SupportsShouldProcess = true, DefaultParameterSetName = "Item")]
    public class ExecuteShellCommandCommand : BaseShellCommand
    {
        [ValidatePattern(@"[\*\?\[\]\-0-9a-zA-Z_]+\:[\*\?\[\]\-0-9a-zA-Z_]+")]
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
                        WriteItem(Item);
                    }
                });
        }
    }
}