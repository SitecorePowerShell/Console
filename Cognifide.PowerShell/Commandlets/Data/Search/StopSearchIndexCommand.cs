using System;
using System.Management.Automation;
using Sitecore.ContentSearch;

namespace Cognifide.PowerShell.Commandlets.Data.Search
{
    [Cmdlet(VerbsLifecycle.Stop, "SearchIndex", DefaultParameterSetName = "Name")]
    [OutputType(typeof(ISearchIndex))]
    public class StopSearchIndexCommand : BaseIndexCommand
    {
        [Parameter(ParameterSetName = "Index", Mandatory = true, ValueFromPipeline = true)]
        [ValidateNotNull]
        public ISearchIndex Index { get; set; }

        protected override void ProcessRecord()
        {
            ISearchIndex searchIndex;

            if (Index != null)
            {
                searchIndex = Index;
            }
            else if (!String.IsNullOrEmpty(Name))
            {
                searchIndex = ContentSearchManager.GetIndex(Name);
            }
            else
            {
                return;
            }

            WriteVerbose($"Stopping index {searchIndex.Name}.");
            searchIndex.StopIndexing();
        }
    }
}