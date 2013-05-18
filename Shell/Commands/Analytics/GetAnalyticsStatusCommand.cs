using System.Data.Objects;
using System.Management.Automation;

namespace Cognifide.PowerShell.Shell.Commands.Analytics
{
    [Cmdlet("Get", "AnalyticsStatus")]
    public class GetAnalyticsStatusCommand : AnalyticsBaseCommand
    {
        protected override void ProcessRecord()
        {
            ObjectQuery<Status> statuses = Context.Status;
            PipeQuery(statuses);
        }
    }
}