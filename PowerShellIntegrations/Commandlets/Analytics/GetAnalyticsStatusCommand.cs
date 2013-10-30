using System.Data.Objects;
using System.Management.Automation;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Analytics
{
    [Cmdlet("Get", "AnalyticsStatus")]
    [OutputType(new[] {typeof (Status)})]
    public class GetAnalyticsStatusCommand : AnalyticsBaseCommand
    {
        protected override void ProcessRecord()
        {
            ObjectQuery<Status> statuses = Context.Status;
            PipeQuery(statuses);
        }
    }
}