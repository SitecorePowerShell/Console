using System.Data.Objects;
using System.Management.Automation;

namespace Cognifide.PowerShell.Shell.Commands.Analytics
{
    [Cmdlet("Get", "AnalyticsTestDefinition")]
    public class GetAnalyticsTestDefinitionsCommand : AnalyticsBaseCommand
    {
        protected override void ProcessRecord()
        {
            ObjectQuery<TestDefinitions> testDefinitions = Context.TestDefinitions;
            PipeQuery(testDefinitions);
        }
    }
}