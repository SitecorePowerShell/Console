using System.Data.Objects;
using System.Management.Automation;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Analytics
{
    [Cmdlet("Get", "AnalyticsPageEvent")]
    public class GetAnalyticsPageEventCommand : AnalyticsBaseCommand
    {
        protected override void ProcessRecord()
        {
            ObjectQuery<PageEvents> pageEvents = Context.PageEvents;
            PipeQuery(pageEvents);
        }
    }
}