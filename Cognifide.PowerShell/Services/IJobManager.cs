using Sitecore;

namespace Cognifide.PowerShell.Services
{
    public interface IJobManager
    {
        Handle StartJob(IJobOptions jobOptions);
    }
}
