using System.Linq;
using System.Management.Automation;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Analytics
{
    [Cmdlet("Get", "AnalyticsVisits")]
    public class GetAnalyticsVisitsCommand : AnalyticsBaseCommand
    {
        [Parameter(ValueFromPipeline = true)]
        public Visits Visit { get; set; }

        [Parameter(ValueFromPipeline = true)]
        public Visitors Visitor { get; set; }

        [Parameter(ValueFromPipeline = true)]
        public Browsers Browser { get; set; }

        [Parameter(ValueFromPipeline = true)]
        public ReferringSites ReferringSite { get; set; }

        protected override void ProcessRecord()
        {
            IQueryable<Visits> visits = Context.Visits;
            if (Visit != null)
            {
                visits = visits.Where(v => v.VisitId == Visit.VisitId);
            }

            if (Visitor != null)
            {
                visits = visits.Where(v => v.Visitors.VisitorId == Visitor.VisitorId);
            }

            if (Browser != null)
            {
                visits = visits.Where(v => v.Browsers.BrowserId == Browser.BrowserId);
            }

            if (ReferringSite != null)
            {
                visits = visits.Where(v => v.ReferringSites.ReferringSiteId == ReferringSite.ReferringSiteId);
            }

            PipeQuery(visits);
        }
    }
}