using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Web;

namespace Cognifide.PowerShell.Commandlets.Jobs
{
    [Cmdlet(VerbsCommon.Get, "Jobs")]
    [OutputType(typeof (Sitecore.Jobs.Job))]
    public class GetJobsCommand : BaseCommand
    {
        protected override void ProcessRecord()
        {
            var jobs = Sitecore.Jobs.JobManager.GetJobs();
            foreach (var job in jobs)
            {
                WriteObject(job);
            }
        }
    }
}