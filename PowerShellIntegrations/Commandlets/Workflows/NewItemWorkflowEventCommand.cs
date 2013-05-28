using System.Linq;
using System.Management.Automation;
using Sitecore.Data.Items;
using Sitecore.Workflows;
using Sitecore.Workflows.Simple;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Workflows
{
    [Cmdlet("New", "ItemWorkflowEvent", SupportsShouldProcess = true, DefaultParameterSetName = "Item")]
    public class NewItemWorkflowEventCommand : BaseCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public Item Item { get; set; }

        [Parameter]
        public string Path { get; set; }

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public string OldState { get; set; }

        [Parameter]
        public string NewState { get; set; }

        [Parameter]
        public string Text { get; set; }

        protected override void ProcessRecord()
        {
            Item = FindItemFromParameters(Item, Path, Id);

            WorkflowEvent lastEvent =
                ((WorkflowProvider) Item.Database.WorkflowProvider).HistoryStore.GetHistory(Item)
                                                                   .OrderBy(p => p.Date)
                                                                   .Last();
            ((WorkflowProvider) Item.Database.WorkflowProvider).HistoryStore.AddHistory(
                Item,
                string.IsNullOrEmpty(OldState) ? lastEvent.NewState : OldState,
                string.IsNullOrEmpty(NewState) ? lastEvent.NewState : NewState,
                Text
                );
        }
    }
}