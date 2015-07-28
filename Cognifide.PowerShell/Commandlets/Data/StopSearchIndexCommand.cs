using System;
using System.Management.Automation;
using Sitecore.ContentSearch;

namespace Cognifide.PowerShell.Commandlets.Data
{
    [Cmdlet(VerbsLifecycle.Stop, "SearchIndex", DefaultParameterSetName = "Name")]
    [OutputType(typeof(ISearchIndex))]
    public class StopSearchIndexCommand : BaseIndexCommand
    {
        protected override void ProcessRecord()
        {
            if (Name == null) return;

            WriteVerbose(String.Format("Stopping index {0}.", Name));
            ContentSearchManager.GetIndex(Name).StopIndexing();
        }
    }
}