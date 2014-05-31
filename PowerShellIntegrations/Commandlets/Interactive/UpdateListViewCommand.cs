using System.Management.Automation;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive
{
    [Cmdlet("Update", "ListView")]
    [OutputType(new[] {typeof (string)})]
    public class UpdateListViewCommand : BaseListViewCommand
    {
        protected override void EndProcessing()
        {
            LogErrors(() => SessionState.PSVariable.Set("allData", cumulativeData));
            base.EndProcessing();
        }
    }
}