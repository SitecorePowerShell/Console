using System.Linq;
using System.Management.Automation;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Maintenance;
using Sitecore.ContentSearch.SearchTypes;
using Sitecore.Data.Items;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Spe.Core.Extensions;
using Spe.Core.VersionDecoupling;

namespace Spe.Commands.Data.Search
{
    [Cmdlet(VerbsData.Update, "SearchIndexItem", DefaultParameterSetName = "Name")]
    public class UpdateSearchIndexItemCommand : BaseIndexItemCommand
    {
        protected override void ProcessIndexable(ISearchIndex index, IIndexable indexable, string itemPath)
        {
            WriteVerbose($"Starting index update for item {itemPath} in {index.Name}.");
            var indexManager = TypeResolver.ResolveFromCache<IIndexManager>();
            var job = indexManager.UpdateItem(index, indexable.UniqueId);

            if (job == null || !AsJob) return;

            WriteVerbose($"Background job created: {job.Name}");
            WriteObject(job);
        }

        // For updating items in the index, the index is valid if it has a crawler for the same database.
        protected override bool IndexIsValidForItem(ISearchIndex index, Item item)
        {
            return index.Crawlers.Any(c =>
                c is SitecoreItemCrawler && ((SitecoreItemCrawler) c).Database.Is(item.Database.Name));
        }
    }
}