using System.Collections.Generic;
using Sitecore;
using Sitecore.Jobs;
using Spe.Abstractions.VersionDecoupling.Interfaces;

namespace Spe.VersionSpecific.Services
{
    public class SpeJobManager : IJobManager
    {
        public Handle StartJob(IJobOptions jobOptions)
        {
            var options = jobOptions as SpeJobOptions;
            return JobManager.Start(options).Handle;
        }

        public IJob GetJob(Handle handle)
        {
            var job = JobManager.GetJob(handle);
            return job == null ? null : new SpeJob(job);
        }

        public IJob GetJob(string jobName)
        {
            var job = JobManager.GetJob(jobName);
            return job == null ? null : new SpeJob(job);
        }

        public IEnumerable<object> GetBaseJobs()
        {
            var jobs = JobManager.GetJobs();
            foreach (var job in jobs)
            {
                yield return job;
            }
        }

        public IJob GetContextJob()
        {
            var job = Context.Job;
            return job == null ? null : new SpeJob(job);
        }

        public void SetContextJob(IJob job)
        {
            Context.Job = (job as SpeJob).Job;
        }
    }
}