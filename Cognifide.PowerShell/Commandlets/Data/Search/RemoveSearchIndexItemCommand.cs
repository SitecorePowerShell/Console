using System;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Maintenance;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Data.Search
{
    [Cmdlet(VerbsCommon.Remove, "SearchIndexItem", DefaultParameterSetName = "Name")]
    public class RemoveSearchIndexItem : BaseIndexCommand
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Name")]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Item")]
        public Item Item { get; set; }

        [Parameter]
        public SwitchParameter AsJob { get; set; }

        protected override void ProcessRecord()
        {
            var itemDatabase = Item.Database.Name;
            foreach (var index in WildcardFilter(Name, ContentSearchManager.Indexes, index => index.Name))
            {
                if (!index.Crawlers.Any(c => c is SitecoreItemCrawler && ((SitecoreItemCrawler)c).Database.Is(itemDatabase))) continue;

                WriteVerbose($"Removing item {Item.Paths.Path} from index {index.Name}.");
                var job = IndexCustodian.DeleteItem(index, new SitecoreIndexableItem(Item).Id);

                if (job != null && AsJob)
                {
                    WriteVerbose($"Background job created: {job.Name}");
                    WriteObject(job);
                }
            }
        }
    }
}