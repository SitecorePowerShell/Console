using System.Data.Objects;
using System.Management.Automation;

namespace Cognifide.PowerShell.Shell.Commands.Analytics
{
    [Cmdlet("Get", "AnalyticsAutomationState")]
    public class GetAnalyticsAutomationStateCommand : AnalyticsBaseCommand
    {
        protected override void ProcessRecord()
        {
            ObjectQuery<AutomationStates> automationStates = Context.AutomationStates;

            PipeQuery(automationStates);
        }
    }
}