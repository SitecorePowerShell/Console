using System.Data.Objects;
using System.Management.Automation;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Analytics
{
    [Cmdlet("Get", "AnalyticsKeyword")]
    [OutputType(new[] {typeof (Keywords)})]
    public class GetAnalyticsKeywordCommand : AnalyticsBaseCommand
    {
        protected override void ProcessRecord()
        {
            ObjectQuery<Keywords> keywords = Context.Keywords;

            PipeQuery(keywords);
        }
    }
}