using System.Data.Objects;
using System.Management.Automation;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Analytics
{
    [Cmdlet("Get", "AnalyticsReferringSites")]
    [OutputType(new[] { typeof(ReferringSites) })]
    public class GetAnalyticsReferringSiteCommand : AnalyticsBaseCommand
    {
        protected override void ProcessRecord()
        {
            ObjectQuery<ReferringSites> referringSites = Context.ReferringSites;
            PipeQuery(referringSites);
        }
    }
}