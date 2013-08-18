using System.Linq;
using System.Management.Automation;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Analytics
{
    [Cmdlet("Get", "AnalyticsLocation", DefaultParameterSetName = "BusinessName")]
    [OutputType(new[] { typeof(Locations) })]
    public class GetAnalyticsLocationCommand : AnalyticsBaseCommand
    {
        [Parameter(ValueFromPipeline = true, Position = 0)]
        public string BusinessName { get; set; }

        [Parameter(ValueFromPipeline = true, Position = 0)]
        public string Country { get; set; }

        protected override void ProcessRecord()
        {
            IQueryable<Locations> locations = Context.Locations;
            if (!string.IsNullOrEmpty(BusinessName))
            {
                locations = locations.Where(l => l.BusinessName.Contains(BusinessName));
            }

            if (!string.IsNullOrEmpty(Country))
            {
                locations = locations.Where(l => l.Country == Country);
            }

            PipeQuery(locations);
        }
    }
}