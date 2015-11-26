using System;
using System.Management.Automation;
using Sitecore.ContentSearch;

namespace Cognifide.PowerShell.Commandlets.Data.Search
{
    [Cmdlet(VerbsLifecycle.Suspend, "SearchIndex", DefaultParameterSetName = "Name")]
    [OutputType(typeof(ISearchIndex))]
    public class SuspendSearchIndexCommand : BaseIndexCommand
    {
        protected override void ProcessRecord()
        {
            if (Name == null) return;

            WriteVerbose($"Pausing index {Name}.");
            ContentSearchManager.GetIndex(Name).PauseIndexing();
        }
    }
}