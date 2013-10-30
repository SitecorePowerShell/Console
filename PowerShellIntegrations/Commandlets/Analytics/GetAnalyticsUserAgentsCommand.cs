using System.Data.Objects;
using System.Management.Automation;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Analytics
{
    [Cmdlet("Get", "AnalyticsUserAgent")]
    [OutputType(new[] { typeof(UserAgents) })]
    public class GetAnalyticsUserAgentsCommand : AnalyticsBaseCommand
    {
        protected override void ProcessRecord()
        {
            ObjectQuery<UserAgents> userAgents = Context.UserAgents;
            PipeQuery(userAgents);
        }
    }
}