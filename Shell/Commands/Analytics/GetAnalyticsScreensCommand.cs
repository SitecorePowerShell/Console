using System.Data.Objects;
using System.Management.Automation;

namespace Cognifide.PowerShell.Shell.Commands.Analytics
{
    [Cmdlet("Get", "AnalyticsScreen")]
    public class GetAnalyticsScreensCommand : AnalyticsBaseCommand
    {
        protected override void ProcessRecord()
        {
            ObjectQuery<Screens> screens = Context.Screens;
            PipeQuery(screens);
        }
    }
}