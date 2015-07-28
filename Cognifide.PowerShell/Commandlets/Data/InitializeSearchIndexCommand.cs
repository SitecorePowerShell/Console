using System;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Maintenance;

namespace Cognifide.PowerShell.Commandlets.Data
{
    [Cmdlet(VerbsData.Initialize, "SearchIndex", DefaultParameterSetName = "Name")]
    public class InitializeSearchIndexCommand : BaseIndexCommand
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0, ParameterSetName = "Instance")]
        public ISearchIndex Index { get; set; }

        [Parameter]
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
                WriteVerbose(String.Format("Skipping full index rebuild for {0} because it's already running.", index.Name));
                return;
            }

            const string message = "Starting full index rebuild for {0}.";
            WriteVerbose(String.Format(message, index.Name));
            var job = (isRemoteIndex) ? IndexCustodian.FullRebuildRemote(index) : IndexCustodian.FullRebuild(index);
            if (job == null) return;

            WriteVerbose(String.Format("Background job created: {0}", job.Name));

            if (AsJob)
            {
                WriteObject(job);
            }
        }
    }
}