using System.Collections.Generic;
using System.Management.Automation;
using System.Xml;
using Cognifide.PowerShell.Commandlets.Interactive.Messages;
using Cognifide.PowerShell.Core.Validation;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Interactive
{
    [Cmdlet(VerbsLifecycle.Invoke, "ShellCommand")]
    [OutputType(typeof (Item))]
    public class InvokeShellCommandCommand : BaseItemCommand
    {
        [Parameter(Position = 0, Mandatory = true)]
        [AutocompleteSet(nameof(CommandNames))]
        public string Name { get; set; }

        public static string[] CommandNames = GetAllCommands();

        private static string[] GetAllCommands()
        {
            var commands = Factory.GetConfigNodes("commands/command");
            var commandNames = new List<string>();
            foreach (XmlElement command in commands)
            {
                try
                {
                    commandNames.Add(command.Attributes["name"].Value);
                }
                catch
                {
                    // not really relevant to report this problem here ...
                }
            }
            return commandNames.ToArray();
        }


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