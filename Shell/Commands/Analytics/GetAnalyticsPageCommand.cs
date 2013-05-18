using System.Data.Objects;
using System.Management.Automation;

namespace Cognifide.PowerShell.Shell.Commands.Analytics
{
    [Cmdlet("Get", "AnalyticsPage")]
    public class GetAnalyticsPageCommand : AnalyticsBaseCommand
    {
        protected override void ProcessRecord()
        {
            ObjectQuery<Pages> pages = Context.Pages;
            PipeQuery(pages);
        }
    }
}