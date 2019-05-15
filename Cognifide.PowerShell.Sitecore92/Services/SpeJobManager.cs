using Cognifide.PowerShell.Services;
using Sitecore;
using Sitecore.Jobs;

namespace Cognifide.PowerShell.VersionSpecific.Services
{
    public class SpeJobManager : IJobManager
    {
        public Handle StartJob(IJobOptions jobOptions)
        {
            var options = jobOptions as SpeJobOptions;
            return JobManager.Start(options).Handle;
        }
    }
}