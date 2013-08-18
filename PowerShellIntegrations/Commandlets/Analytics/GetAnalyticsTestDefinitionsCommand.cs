using System.Data.Objects;
using System.Management.Automation;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Analytics
{
    [Cmdlet("Get", "AnalyticsTestDefinition")]
    [OutputType(new[] { typeof(TestDefinitions) })]
    public class GetAnalyticsTestDefinitionsCommand : AnalyticsBaseCommand
    {
        protected override void ProcessRecord()
        {
            ObjectQuery<TestDefinitions> testDefinitions = Context.TestDefinitions;
            PipeQuery(testDefinitions);
        }
    }
}