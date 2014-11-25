using Sitecore.Pipelines.Logout;

namespace Cognifide.PowerShell.Pipelines.Logout
{
    public class LogoutScript : PipelineProcessor<LogoutArgs>
    {
        protected override string IntegrationPoint { get { return "pipelineLogout"; } }        
    }
}