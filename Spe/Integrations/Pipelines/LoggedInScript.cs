using Sitecore.Pipelines.LoggedIn;
using Spe.Core.Modules;

namespace Spe.Integrations.Pipelines
{
    public class LoggedInScript : PipelineProcessor<LoggedInArgs>
    {
        protected override string IntegrationPoint => IntegrationPoints.PipelineLoggedInFeature;
    }
}