using Cognifide.PowerShell.Core.Modules;
using Sitecore.Pipelines.Logout;

namespace Cognifide.PowerShell.Integrations.Pipelines
{
    public class LogoutScript : PipelineProcessor<LogoutArgs>
    {
        protected override string IntegrationPoint => IntegrationPoints.PipelineLogoutFeature;
    }
}