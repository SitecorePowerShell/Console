using System;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.VersionDecoupling;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Maintenance;
using Sitecore.ContentSearch.SearchTypes;
using Sitecore.Data.Items;
using static Cognifide.PowerShell.Core.Extensions.CmdletExtensions;

namespace Cognifide.PowerShell.Commandlets.Data.Search
{
    [Cmdlet(VerbsData.Initialize, "SearchIndexItem", DefaultParameterSetName = "Name")]
    public class InitializeSearchIndexItemCommand : BaseIndexCommand
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Name")]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Item")]
        public Item Item { get; set; }

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "SearchResultItem")]
        public SearchResultItem SearchResultItem { get; set; }

        [Parameter]
        public SwitchParameter AsJob { get; set; }

        protected override void ProcessRecord()
        {
            SitecoreVersion.V72.OrNewer(
                () =>
                {
                    if (Item != null)
                    {
                        var itemDatabase = Item.Database.Name;
                        var itemPath = Item.Paths.Path;
                        var indexable = new SitecoreIndexableItem(Item);

                        foreach (var index in WildcardFilter(Name, ContentSearchManager.Indexes, index => index.Name))
                        {
                            if (
                                !index.Crawlers.Any(
                                    c => c is SitecoreItemCrawler && ((SitecoreItemCrawler) c).Database.Is(itemDatabase)))
                                continue;

                            RefreshItem(index, indexable, itemPath);
                        }
                    }
                    else if (SearchResultItem != null)
                    {
                        var itemPath = SearchResultItem.Path;
                        var indexable = new SitecoreIndexableItem(SearchResultItem.GetItem());
                        var indexname = SearchResultItem.Fields["_indexname"].ToString();

                        foreach (
                            var index in WildcardFilter(indexname, ContentSearchManager.Indexes, index => index.Name))
                        {
                            RefreshItem(index, indexable, itemPath);
                        }
                    }
                })
                .ElseWriteWarning(this, "Initialize-SearchIndexItem", false);
        }


        private void RefreshItem(ISearchIndex index, IIndexable indexable, string itemPath)
        {
            WriteVerbose($"Starting index rebuild for item {itemPath} in {index.Name}.");
            var job = IndexCustodian.Refresh(index, indexable);

            if (job != null && AsJob)
            {
                WriteVerbose($"Background job created: {job.Name}");
                WriteObject(job);
            }
        }
    }
}