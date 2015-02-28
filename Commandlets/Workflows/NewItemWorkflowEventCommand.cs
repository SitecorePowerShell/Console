using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Data.Items;
using Sitecore.Workflows.Simple;

namespace Cognifide.PowerShell.Commandlets.Workflows
{
    [Cmdlet(VerbsCommon.New, "ItemWorkflowEvent", SupportsShouldProcess = true)]
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
            var lastEvent =
                ((WorkflowProvider) item.Database.WorkflowProvider).HistoryStore.GetHistory(item)
                    .OrderBy(p => p.Date)
                    .LastOrDefault();
            if (string.IsNullOrEmpty(OldState))
            {
                OldState = lastEvent != null ? lastEvent.NewState : string.Empty;
            }

            if (string.IsNullOrEmpty(NewState))
            {
                NewState = lastEvent != null ? lastEvent.NewState : string.Empty;
            }

            if (ShouldProcess(item.GetProviderPath(), string.Format("Add '{0}' workflow history entry.", Text)))
            {
                ((WorkflowProvider) item.Database.WorkflowProvider).HistoryStore.AddHistory(
                    item, OldState, NewState, Text);
            }
        }
    }
}