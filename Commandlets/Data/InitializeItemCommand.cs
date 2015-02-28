using System.Management.Automation;
using Sitecore.ContentSearch.SearchTypes;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Data
{
    [Cmdlet(VerbsData.Initialize, "Item")]
    [OutputType(typeof (Item))]
    public class InitializeItemCommand : BaseCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "From Item",
            Mandatory = true)]
        public Item Item { get; set; }

        [Parameter(ValueFromPipeline = true, ParameterSetName = "From Search Result Item", Mandatory = true)]
        public SearchResultItem SearchResultItem { get; set; }

        protected override void ProcessRecord()
        {
            if (Item != null)
            {
                WriteItem(Item);
            }

            if (SearchResultItem != null)
            {
                WriteItem(SearchResultItem.GetItem());
            }
        }
    }
}