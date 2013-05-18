using System.Data.Objects;
using System.Management.Automation;

namespace Cognifide.PowerShell.Shell.Commands.Analytics
{
    [Cmdlet("Get", "AnalyticsUserAgent")]
    public class GetAnalyticsUserAgentsCommand : AnalyticsBaseCommand
    {
        protected override void ProcessRecord()
        {
            ObjectQuery<UserAgents> userAgents = Context.UserAgents;
            PipeQuery(userAgents);
        }
    }
}