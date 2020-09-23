using Sitecore.ContentSearch;

namespace Spe.Abstractions.VersionDecoupling.Interfaces
{
    public interface IIndexManager
    {
        IJob DeleteItem(ISearchIndex index, IIndexableId indexableId);
        IJob UpdateItem(ISearchIndex index, IIndexableUniqueId indexableId);
        IJob Refresh(ISearchIndex index, IIndexable indexable);
        IJob FullRebuild(ISearchIndex index, bool isRemote);
    }
}
