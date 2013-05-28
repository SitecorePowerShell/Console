using System.Linq;
using System.Management.Automation;
using Sitecore.Data.Items;
using Sitecore.Exceptions;
using Sitecore.Workflows;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Workflows
{
    [Cmdlet("Execute", "Workflow")]
    public class ExecuteWorkflowCommand : BaseCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public Item Item { get; set; }

        [Parameter(Position = 0)]
        public string Path { get; set; }

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public string CommandName { get; set; }

        [Parameter]
        public string Comments { get; set; }

        protected override void ProcessRecord()
        {
            Item = FindItemFromParameters(Item, Path, Id);

            IWorkflowProvider workflowProvider = Item.Database.WorkflowProvider;
            if (workflowProvider == null)
            {
                throw new WorkflowException("Workflow provider could not be obtained for database: " +
                                            Item.Database.Name);
            }

            IWorkflow workflow = workflowProvider.GetWorkflow(Item);
            if (workflow == null)
            {
                throw new WorkflowException("Workflow missing or item not in workflow: " + Item.ID);
            }

            try
            {
                WorkflowCommand command = workflow.GetCommands(Item).FirstOrDefault(c => c.DisplayName == CommandName);
                if (command == null)
                {
                    throw new WorkflowException("Command not present or no execution rights: " + CommandName);
                }

                WorkflowResult workflowResult = workflow.Execute(command.CommandID, Item, Comments, false, new object[0]);
                if (!workflowResult.Succeeded)
                {
                    string message = workflowResult.Message;
                    if (string.IsNullOrEmpty(message))
                    {
                        message = "IWorkflow.Execute() failed for unknown reason.";
                    }
                    throw new WorkflowException(message);
                }
            }
            catch (WorkflowStateMissingException)
            {
                throw new WorkflowStateMissingException("Item workflow state does not specify the next step.");
            }
        }
    }
}