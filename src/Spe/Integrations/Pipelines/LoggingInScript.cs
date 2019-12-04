using Sitecore.Pipelines.LoggingIn;
using Spe.Core.Modules;

namespace Spe.Integrations.Pipelines
{
    public class LoggingInScript : PipelineProcessor<LoggingInArgs>
    {
        protected override string IntegrationPoint => IntegrationPoints.PipelineLoggingInFeature;
    }
}