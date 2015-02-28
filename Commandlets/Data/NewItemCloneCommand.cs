using System.Management.Automation;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Data
{
    [Cmdlet("New", "ItemClone", SupportsShouldProcess = true)]
    [OutputType(typeof (Item), ParameterSetName = new[] {"Item from Pipeline", "Item from Path", "Item from ID"})]
    public class NewItemCloneCommand : BaseLanguageAgnosticItemCommand
    {
        [Parameter(Mandatory = true)]
        public Item Destination { get; set; }

        [Parameter]
        public string Name { get; set; }

        [Parameter]
        public SwitchParameter Recursive { get; set; }

        protected override void ProcessItem(Item item)
        {
            var name = string.IsNullOrEmpty(Name) ? item.Name : Name;
            if (ShouldProcess(Destination.GetProviderPath(),
                string.Format("Create clone of '{0}'  with name '{1}' {2} children", item.GetProviderPath(), name,
                    (Recursive.IsPresent ? "with" : "without"))))
            {
                var clone = item.CloneTo(Destination, name, Recursive.IsPresent);
                WriteItem(clone);
            }
        }
    }
}