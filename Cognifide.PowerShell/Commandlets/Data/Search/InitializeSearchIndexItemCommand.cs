using System;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Maintenance;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Data.Search
{
    [Cmdlet(VerbsData.Initialize, "SearchIndexItem", DefaultParameterSetName = "Name")]
    public class InitializeSearchIndexItemCommand : BaseCommand
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Name")]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Item")]
        public Item Item { get; set; }

        [Parameter]
        public SwitchParameter AsJob { get; set; }

        protected override void ProcessRecord()
        {
            var itemDatabase = Item.Database.Name;
            foreach (var index in ContentSearchManager.Indexes)
            {
                if (!index.Crawlers.Any(c => c is SitecoreItemCrawler && ((SitecoreItemCrawler)c).Database.Is(itemDatabase))) continue;

                WriteVerbose($"Starting index rebuild for item {Item.Paths.Path} in {index.Name}.");
                var job = IndexCustodian.Refresh(index, new SitecoreIndexableItem(Item));

                if (job != null && AsJob)
                {
                    WriteVerbose($"Background job created: {job.Name}");
                    WriteObject(job);
                }
            }
        }
    }
}