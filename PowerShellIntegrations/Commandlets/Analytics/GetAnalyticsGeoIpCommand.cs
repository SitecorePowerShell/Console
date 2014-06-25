using System.Data.Objects;
using System.Management.Automation;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Analytics
{
    [Cmdlet(VerbsCommon.Get, "AnalyticsGeoIP")]
    [OutputType(new[] {typeof (GeoIps)})]
    public class GetAnalyticsGeoIPCommand : AnalyticsBaseCommand
    {
        protected override void ProcessRecord()
        {
            ObjectQuery<GeoIps> geoIps = Context.GeoIps;
            PipeQuery(geoIps);
        }
    }
}