using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Maintenance;

namespace Cognifide.PowerShell.Commandlets.Data.Search
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
                return;
            }

            WriteVerbose($"Starting full index rebuild for {index.Name}.");
            var job = (isRemoteIndex) ? IndexCustodian.FullRebuildRemote(index) : IndexCustodian.FullRebuild(index);

            if (job != null && AsJob)
            {
                WriteVerbose($"Background job created: {job.Name}");
                WriteObject(job);
            }
        }
    }
}