using System.Management.Automation;

using Sitecore.Data.Items;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Data
{
    [Cmdlet("New", "ItemClone")]
    [OutputType(new[] { typeof(Item) }, ParameterSetName = new[] { "Item from Pipeline", "Item from Path", "Item from ID" })]
    public class NewItemCloneCommand : BaseItemCommand
    {
        [Parameter(Mandatory=true)]
        public Item Destination { get; set; }
        
        [Parameter]
        public string Name { get; set; }

        [Parameter]
        public SwitchParameter Recursive { get; set; }

        protected override void ProcessItem(Item item)
        {
            Item clone;
            if (string.IsNullOrEmpty(Name))
            {
                clone = item.CloneTo(Destination, Recursive.IsPresent);
            }
            else
            {
                clone = item.CloneTo(Destination, Name, Recursive.IsPresent);
            }

            WriteItem(clone);
        }
    }
}