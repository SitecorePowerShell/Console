using System.Collections.Generic;
using Sitecore;

namespace Spe.Abstractions.VersionDecoupling.Interfaces
{
    public interface IJobManager
    {
        Handle StartJob(IJobOptions jobOptions);
        IJob GetJob(Handle handle);
        IJob GetJob(string jobName);
        IEnumerable<object> GetBaseJobs();
        IJob GetContextJob();
        void SetContextJob(IJob job);
    }
}