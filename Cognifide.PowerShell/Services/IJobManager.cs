using Sitecore;

namespace Cognifide.PowerShell.Services
{
    public interface IJobManager
    {
        Handle StartJob(IJobOptions jobOptions);
        IJob GetJob(Handle handle);
        IJob GetJob(string jobName);
        IJob GetContextJob();
        void SetContextJob(IJob job);
    }
}
