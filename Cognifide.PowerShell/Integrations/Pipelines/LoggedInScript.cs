using Cognifide.PowerShell.Core.Modules;
using Sitecore.Pipelines.LoggedIn;

namespace Cognifide.PowerShell.Integrations.Pipelines
{
    public class LoggedInScript : PipelineProcessor<LoggedInArgs>
    {
        protected override string IntegrationPoint => IntegrationPoints.PipelineLoggedInFeature;
    }
}