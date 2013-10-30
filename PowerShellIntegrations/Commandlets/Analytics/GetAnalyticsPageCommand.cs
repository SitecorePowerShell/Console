using System.Data.Objects;
using System.Management.Automation;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Analytics
{
    [Cmdlet("Get", "AnalyticsPage")]
    [OutputType(new[] {typeof (Pages)})]
    public class GetAnalyticsPageCommand : AnalyticsBaseCommand
    {
        protected override void ProcessRecord()
        {
            ObjectQuery<Pages> pages = Context.Pages;
            PipeQuery(pages);
        }
    }
}