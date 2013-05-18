using System.Data.Objects;
using System.Management.Automation;

namespace Cognifide.PowerShell.Shell.Commands.Analytics
{
    [Cmdlet("Get", "AnalyticsVisitor", DefaultParameterSetName = "Name")]
    public class GetAnalyticsVisitorCommand : AnalyticsBaseCommand
    {
        protected override void ProcessRecord()
        {
            ObjectQuery<Visitors> visitors = Context.Visitors;
            PipeQuery(visitors);
        }
    }
}