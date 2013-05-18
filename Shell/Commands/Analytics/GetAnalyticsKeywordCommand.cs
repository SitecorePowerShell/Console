using System.Data.Objects;
using System.Management.Automation;

namespace Cognifide.PowerShell.Shell.Commands.Analytics
{
    [Cmdlet("Get", "AnalyticsKeyword")]
    public class GetAnalyticsKeywordCommand : AnalyticsBaseCommand
    {
        protected override void ProcessRecord()
        {
            ObjectQuery<Keywords> keywords = Context.Keywords;

            PipeQuery(keywords);
        }
    }
}