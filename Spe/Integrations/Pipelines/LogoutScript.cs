using Sitecore.Pipelines.Logout;
using Spe.Core.Modules;

namespace Spe.Integrations.Pipelines
{
    public class LogoutScript : PipelineProcessor<LogoutArgs>
    {
        protected override string IntegrationPoint => IntegrationPoints.PipelineLogoutFeature;
    }
}