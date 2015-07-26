using System;
using System.Management.Automation;
using Sitecore.ContentSearch;

namespace Cognifide.PowerShell.Commandlets.Data
{
    [Cmdlet(VerbsLifecycle.Resume, "SearchIndex", DefaultParameterSetName = "Name")]
    [OutputType(typeof(ISearchIndex))]
    public class ResumeSearchIndexCommand : BaseCommand
    {
        [ValidatePattern("[\\*\\?\\[\\]\\-0-9a-zA-Z_]+")]
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0, ParameterSetName = "Name")]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            if (Name == null) return;

            WriteVerbose(String.Format("Resuming index {0}.", Name));
            ContentSearchManager.GetIndex(Name).ResumeIndexing();
        }
    }
}