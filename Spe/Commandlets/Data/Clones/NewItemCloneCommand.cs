using System.Management.Automation;
using Sitecore.Data.Items;
using Spe.Core.Utility;

namespace Spe.Commandlets.Data.Clones
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
        [Alias("Recursive")]
        public SwitchParameter Recurse { get; set; }

        protected override void ProcessItem(Item item)
        {
            var name = string.IsNullOrEmpty(Name) ? item.Name : Name;
            if (ShouldProcess(Destination.GetProviderPath(),
                $"Create clone of '{item.GetProviderPath()}'  with name '{name}' {(Recurse ? "with" : "without")} children"))
            {
                var clone = item.CloneTo(Destination, name, Recurse.IsPresent);
                WriteItem(clone);
            }
        }
    }
}