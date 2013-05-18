using System.Data.Objects;
using System.Management.Automation;

namespace Cognifide.PowerShell.Shell.Commands.Analytics
{
    [Cmdlet("Get", "AnalyticsTrafficType")]
    public class GetAnalyticsTrafficTypesCommand : AnalyticsBaseCommand
    {
        protected override void ProcessRecord()
        {
            ObjectQuery<TrafficTypes> trafficTypes = Context.TrafficTypes;
            PipeQuery(trafficTypes);
        }
    }
}