using System;
using System.Linq;
using System.Management.Automation;
using Sitecore.ContentSearch;

namespace Cognifide.PowerShell.Commandlets.Data
{
    [Cmdlet(VerbsLifecycle.Resume, "SearchIndex", DefaultParameterSetName = "Name")]
    [OutputType(typeof(ISearchIndex))]
    public class ResumeSearchIndexCommand : BaseCommand
    {
        private readonly string[] indexes = ContentSearchManager.Indexes.Select(i => i.Name).ToArray();

        [ValidateSet("*")]
        [Parameter(ValueFromPipeline = true, Position = 0, ParameterSetName = "Name")]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            if (Name == null) return;

            WriteVerbose(String.Format("Resuming index {0}.", Name));
            ContentSearchManager.GetIndex(Name).ResumeIndexing();
        }

        public override object GetDynamicParameters()
        {
            if (!_reentrancyLock.WaitOne(0))
            {
                _reentrancyLock.Set();

                SetValidationSetValues("Name", indexes);

                _reentrancyLock.Reset();
            }

            return base.GetDynamicParameters();
        }
    }
}