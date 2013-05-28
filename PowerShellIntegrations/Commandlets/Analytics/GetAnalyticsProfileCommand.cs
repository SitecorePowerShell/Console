using System.Data.Objects;
using System.Management.Automation;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Analytics
{
    [Cmdlet("Get", "AnalyticsProfile")]
    public class GetAnalyticsProfileCommand : AnalyticsBaseCommand
    {
        protected override void ProcessRecord()
        {
            ObjectQuery<Profiles> profile = Context.Profiles;
            PipeQuery(profile);
        }
    }
}