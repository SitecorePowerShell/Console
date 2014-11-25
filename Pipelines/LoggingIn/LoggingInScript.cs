using Sitecore.Pipelines.LoggingIn;

namespace Cognifide.PowerShell.Pipelines.LoggingIn
{
    public class LoggingInScript : PipelineProcessor<LoggingInArgs>
    {
        protected override string IntegrationPoint { get { return "pipelineLoggingIn"; } }        
    }
}