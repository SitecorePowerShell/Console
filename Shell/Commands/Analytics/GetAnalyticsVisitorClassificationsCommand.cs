using System.Data.Objects;
using System.Management.Automation;

namespace Cognifide.PowerShell.Shell.Commands.Analytics
{
    [Cmdlet("Get", "VisitorClassification")]
    public class GetAnalyticsVisitorClassificationsCommand : AnalyticsBaseCommand
    {
        protected override void ProcessRecord()
        {
            ObjectQuery<VisitorClassifications> visitorClassifications = Context.VisitorClassifications;
            PipeQuery(visitorClassifications);
        }
    }
}