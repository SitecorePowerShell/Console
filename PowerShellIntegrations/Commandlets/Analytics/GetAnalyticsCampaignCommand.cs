using System.Management.Automation;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Analytics
{
    [Cmdlet("Get", "AnalyticsCampaign", DefaultParameterSetName = "Name")]
    [OutputType(new[] { typeof(Campaigns) })]
    public class GetAnalyticsCampaignCommand : AnalyticsBaseCommand
    {
        protected override void ProcessRecord()
        {
            PipeQuery(Context.Campaigns);
        }
    }
}