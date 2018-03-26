using System.Management.Automation;
using Sitecore.StringExtensions;

namespace Cognifide.PowerShell.Commandlets.Jobs
{
    [Cmdlet(VerbsCommon.Get, "SitecoreJob")]
    [OutputType(typeof (Sitecore.Jobs.Job))]
    public class GetSitecoreJobCommand : BaseCommand
    {
        [Parameter]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            if (Name.IsNullOrEmpty())
            {
                var jobs = Sitecore.Jobs.JobManager.GetJobs();
                WriteObject(jobs, true);
            }
            else
            {
                var job = Sitecore.Jobs.JobManager.GetJob(Name);
                WriteObject(job);
            }
        }
    }
}