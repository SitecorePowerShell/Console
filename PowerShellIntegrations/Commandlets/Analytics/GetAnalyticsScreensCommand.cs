using System.Data.Objects;
using System.Management.Automation;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Analytics
{
    [Cmdlet(VerbsCommon.Get, "AnalyticsScreen")]
    [OutputType(new[] {typeof (Screens)})]
    public class GetAnalyticsScreensCommand : AnalyticsBaseCommand
    {
        protected override void ProcessRecord()
        {
            ObjectQuery<Screens> screens = Context.Screens;
            PipeQuery(screens);
        }
    }
}