using System.Data.Objects;
using System.Management.Automation;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Analytics
{
    [Cmdlet("Get", "AnalyticsPageEventDefinitions")]
    public class GetAnalyticsPageEventDefinitionsCommand : AnalyticsBaseCommand
    {
        protected override void ProcessRecord()
        {
            ObjectQuery<PageEventDefinitions> pageEventDefinitions = Context.PageEventDefinitions;
            PipeQuery(pageEventDefinitions);
        }
    }
}