using System.Management.Automation;
using Sitecore.Data.Items;
using Sitecore.Workflows;
using Sitecore.Workflows.Simple;

namespace Cognifide.PowerShell.Commandlets.Workflows
{
    [Cmdlet(VerbsCommon.Get, "ItemWorkflowEvent")]
    [OutputType(typeof (WorkflowEvent))]
    public class GetItemWorkflowEventCommand : BaseItemCommand
    {
        [Parameter]
        [Alias("UserName", "User")]
        public string Identity { get; set; }

        protected override void ProcessItem(Item item)
        {
            if (string.IsNullOrEmpty(Identity))
            {
                Identity = "*";
            }
            var workflowHistory =
                ((WorkflowProvider) item.Database.WorkflowProvider).HistoryStore.GetHistory(item);
            WildcardWrite(Identity, workflowHistory, workflowEvent => workflowEvent.User);
        }
    }
}