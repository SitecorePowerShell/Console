using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Maintenance;
using Spe.Abstractions.VersionDecoupling.Interfaces;

namespace Spe.VersionSpecific.Services
{
    public class SpeIndexManager : IIndexManager
    {
        public IJob DeleteItem(ISearchIndex index, IIndexableId indexableId)
        {
            var job = IndexCustodian.DeleteItem(index, indexableId);
            return job == null ? null : new SpeJob(job);
        }

        public IJob Refresh(ISearchIndex index, IIndexable indexable)
        {
            var job = IndexCustodian.Refresh(index, indexable);
            return job == null ? null : new SpeJob(job);
        }

        public IJob FullRebuild(ISearchIndex index, bool isRemote)
        {
            var job = (isRemote) ? IndexCustodian.FullRebuildRemote(index) : IndexCustodian.FullRebuild(index);
            return job == null ? null : new SpeJob(job);
        }
    }
}
