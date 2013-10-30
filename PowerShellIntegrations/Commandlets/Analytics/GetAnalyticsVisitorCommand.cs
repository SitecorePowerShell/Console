using System.Data.Objects;
using System.Management.Automation;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Analytics
{
    [Cmdlet("Get", "AnalyticsVisitor")]
    [OutputType(new[] { typeof(Visitors) })]
    public class GetAnalyticsVisitorCommand : AnalyticsBaseCommand
    {
        protected override void ProcessRecord()
        {
            ObjectQuery<Visitors> visitors = Context.Visitors;
            PipeQuery(visitors);
        }
    }
}