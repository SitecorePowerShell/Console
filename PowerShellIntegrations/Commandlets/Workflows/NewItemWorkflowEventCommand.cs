using System.Linq;
using System.Management.Automation;
using Sitecore.Data.Items;
using Sitecore.Workflows;
using Sitecore.Workflows.Simple;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Workflows
{
    [Cmdlet("New", "ItemWorkflowEvent")]
    public class NewItemWorkflowEventCommand : BaseItemCommand
    {
        [Parameter]
        public string OldState { get; set; }

        [Parameter]
        public string NewState { get; set; }

        [Parameter]
        public string Text { get; set; }

        protected override void ProcessItem(Item item)
        {

            WorkflowEvent lastEvent =
                ((WorkflowProvider) item.Database.WorkflowProvider).HistoryStore.GetHistory(item)
                    .OrderBy(p => p.Date)
                    .Last();
            ((WorkflowProvider) Item.Database.WorkflowProvider).HistoryStore.AddHistory(
                item,
                string.IsNullOrEmpty(OldState) ? lastEvent.NewState : OldState,
                string.IsNullOrEmpty(NewState) ? lastEvent.NewState : NewState,
                Text
                );
        }
    }
}