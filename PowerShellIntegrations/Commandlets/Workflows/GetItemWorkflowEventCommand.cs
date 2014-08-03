using System.Management.Automation;
using Cognifide.PowerShell.Security;
using Sitecore.Data.Items;
using Sitecore.Workflows;
using Sitecore.Workflows.Simple;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Workflows
{
    [Cmdlet(VerbsCommon.Get, "ItemWorkflowEvent")]
    [OutputType(new[] {typeof (WorkflowEvent)})]
    public class GetItemWorkflowEventCommand : BaseItemCommand
    {
        [Parameter]
        [Alias(new[] { "UserName", "User" })]
        public string Identity { get; set; }

        protected override void ProcessItem(Item item)
        {
            if (string.IsNullOrEmpty(Identity))
            {
                Identity = "*";
            }
            WorkflowEvent[] workflowHistory =
                ((WorkflowProvider) item.Database.WorkflowProvider).HistoryStore.GetHistory(item);
            WildcardWrite(Identity, workflowHistory, workflowEvent => workflowEvent.User);
        }
    }
}