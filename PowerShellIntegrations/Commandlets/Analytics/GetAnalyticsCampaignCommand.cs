using System.Management.Automation;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Analytics
{
    [Cmdlet(VerbsCommon.Get, "AnalyticsCampaign")]
    [OutputType(new[] {typeof (Campaigns)})]
    public class GetAnalyticsCampaignCommand : AnalyticsBaseCommand
    {
        protected override void ProcessRecord()
        {
            PipeQuery(Context.Campaigns);
        }
    }
}