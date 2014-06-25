using System.Data.Objects;
using System.Management.Automation;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Analytics
{
    [Cmdlet(VerbsCommon.Get, "AnalyticsTrafficType")]
    [OutputType(new[] {typeof (TrafficTypes)})]
    public class GetAnalyticsTrafficTypesCommand : AnalyticsBaseCommand
    {
        protected override void ProcessRecord()
        {
            ObjectQuery<TrafficTypes> trafficTypes = Context.TrafficTypes;
            PipeQuery(trafficTypes);
        }
    }
}