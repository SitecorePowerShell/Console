using System.Management.Automation;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Maintenance;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Spe.Core.VersionDecoupling;

namespace Spe.Commands.Data.Search
{
    [Cmdlet(VerbsCommon.Remove, "SearchIndexItem", DefaultParameterSetName = "Name")]
    public class RemoveSearchIndexItem : BaseIndexItemCommand
    {
        protected override void ProcessIndexable(ISearchIndex index, IIndexable indexable, string itemPath)
        {
            WriteVerbose($"Removing item {itemPath} from index {index.Name}.");
            var indexManager = TypeResolver.ResolveFromCache<IIndexManager>();
            var job = indexManager.DeleteItem(index, indexable.Id);

            if (job == null || !AsJob) return;

            WriteVerbose($"Background job created: {job.Name}");
            WriteObject(job);
        }
    }
}