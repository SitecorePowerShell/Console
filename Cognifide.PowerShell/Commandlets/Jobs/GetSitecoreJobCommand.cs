using System.Management.Automation;
using Cognifide.PowerShell.Abstractions.VersionDecoupling.Interfaces;
using Cognifide.PowerShell.Core.VersionDecoupling;
using Sitecore.StringExtensions;

namespace Cognifide.PowerShell.Commandlets.Jobs
{
    [Cmdlet(VerbsCommon.Get, "SitecoreJob")]
    [OutputType(typeof (PSObject))]
    public class GetSitecoreJobCommand : BaseCommand
    {
        [Parameter]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            var jobManager = TypeResolver.ResolveFromCache<IJobManager>();
            if (Name.IsNullOrEmpty())
            {
                var jobs = jobManager.GetBaseJobs();
                WriteObject(jobs, true);
            }
            else
            {
                var job = jobManager.GetJob(Name);
                WriteObject(job);
            }
        }
    }
}