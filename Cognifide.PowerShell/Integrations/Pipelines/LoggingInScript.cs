using Cognifide.PowerShell.Core.Modules;
using Sitecore.Pipelines.LoggingIn;

namespace Cognifide.PowerShell.Integrations.Pipelines
{
    public class LoggingInScript : PipelineProcessor<LoggingInArgs>
    {
        protected override string IntegrationPoint => IntegrationPoints.PipelineLoggingInFeature;
    }
}