using System.Data.Objects;
using System.Management.Automation;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Analytics
{
    [Cmdlet(VerbsCommon.Get, "AnalyticsOs")]
    [OutputType(new[] {typeof (OS)})]
    public class GetAnalyticsOSCommand : AnalyticsBaseCommand
    {
        protected override void ProcessRecord()
        {
            ObjectQuery<OS> oses = Context.OS;
            PipeQuery(oses);
        }
    }
}