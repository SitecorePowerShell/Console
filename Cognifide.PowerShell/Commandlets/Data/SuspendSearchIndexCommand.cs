using System;
using System.Management.Automation;
using Sitecore.ContentSearch;

namespace Cognifide.PowerShell.Commandlets.Data
{
    [Cmdlet(VerbsLifecycle.Suspend, "SearchIndex", DefaultParameterSetName = "Name")]
    [OutputType(typeof(ISearchIndex))]
    public class SuspendSearchIndexCommand : BaseIndexCommand
    {
        protected override void ProcessRecord()
        {
            if (Name == null) return;

            WriteVerbose(String.Format("Pausing index {0}.", Name));
            ContentSearchManager.GetIndex(Name).PauseIndexing();
        }
    }
}