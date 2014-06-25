using System.Management.Automation;
using Sitecore.Data.Items;
using Sitecore.Workflows;
using Sitecore.Workflows.Simple;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Workflows
{
    [Cmdlet("Get", "ItemWorkflowEvent")]
    [OutputType(new[] {typeof (WorkflowEvent)})]
    public class GetItemWorkflowEventCommand : BaseItemCommand
    {
        [Parameter]
        public string UserName { get; set; }

        protected override void ProcessItem(Item item)
        {
            if (string.IsNullOrEmpty(UserName))
            {
                UserName = "*";
            }
            WorkflowEvent[] workflowHistory =
                ((WorkflowProvider) item.Database.WorkflowProvider).HistoryStore.GetHistory(item);
            WildcardWrite(UserName, workflowHistory, workflowEvent => workflowEvent.User);
        }
    }
}