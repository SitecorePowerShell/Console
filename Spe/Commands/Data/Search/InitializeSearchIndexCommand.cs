using System.Management.Automation;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Maintenance;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Spe.Core.Extensions;
using Spe.Core.VersionDecoupling;

namespace Spe.Commands.Data.Search
{
    [Cmdlet(VerbsData.Initialize, "SearchIndex", DefaultParameterSetName = "Name")]
    public class InitializeSearchIndexCommand : BaseIndexCommand
    {
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ValueFromPipeline = true, ParameterSetName = "Instance")]
        public ISearchIndex Index { get; set; }

        [Parameter(ParameterSetName = "Name")]
        [Parameter(ParameterSetName = "Instance")]
        public SwitchParameter IncludeRemoteIndex { get; set; }

        [Parameter]
        public SwitchParameter AsJob { get; set; }

        protected override void ProcessRecord()
        {
            if (ParameterSetName.Is("Name"))
            {
                foreach (var index in ContentSearchManager.Indexes)
                {
                    if (!index.Name.Is(Name)) continue;

                    RebuildIndex(index);
                    if (IncludeRemoteIndex)
                    {
                        RebuildIndex(index, true);
                    }
                }
            }
            else if (ParameterSetName.Is("Instance") && Index != null)
            {
                RebuildIndex(Index);
                if (IncludeRemoteIndex)
                {
                    RebuildIndex(Index, true);
                }
            }
        }

        private void RebuildIndex(ISearchIndex index, bool isRemoteIndex = false)
        {
            if (IndexCustodian.IsRebuilding(index))
            {
                WriteVerbose($"Skipping full index rebuild for {index.Name} because it's already running.");
                var jobManager = TypeResolver.ResolveFromCache<IJobManager>();
                var job = jobManager.GetJob($"Index_Update_IndexName={index.Name}");

                if (job == null || !AsJob) return;

                WriteVerbose($"Background job existed: {job.Name}");
                WriteObject(job);
            }
            else
            {
                WriteVerbose($"Starting full index rebuild for {index.Name}.");
                var job = (isRemoteIndex) ? IndexCustodian.FullRebuildRemote(index) : IndexCustodian.FullRebuild(index);

                if (job == null || !AsJob) return;

                WriteVerbose($"Background job created: {job.Name}");
                WriteObject(job);
            }
        }
    }
}