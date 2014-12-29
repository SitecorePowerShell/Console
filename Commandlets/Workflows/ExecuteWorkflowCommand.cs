using System.Linq;
using System.Management.Automation;
using Sitecore.Data.Items;
using Sitecore.Exceptions;
using Sitecore.Workflows;

namespace Cognifide.PowerShell.Commandlets.Workflows
{
    [Cmdlet("Execute", "Workflow")]
    public class ExecuteWorkflowCommand : BaseItemCommand
    {

        [Parameter]
        public string CommandName { get; set; }

        [Parameter]
        public string Comments { get; set; }

        protected override void ProcessItem(Item item)
        {
            IWorkflowProvider workflowProvider = item.Database.WorkflowProvider;
            if (workflowProvider == null)
            {
                WriteError(
                    new ErrorRecord(new WorkflowException("Workflow provider could not be obtained for database: " +
                                                          item.Database.Name),
                        "sitecore_workflow_provider_missing",
                        ErrorCategory.ObjectNotFound, null));
                return;
            }

            IWorkflow workflow = workflowProvider.GetWorkflow(item);
            if (workflow == null)
            {
                WriteError(new ErrorRecord(
                    new WorkflowException("Workflow missing or item not in workflow: " + item.ID),
                    "sitecore_workflow_missing",
                    ErrorCategory.ObjectNotFound, null));
                return;
            }

            try
            {
                WorkflowCommand command = workflow.GetCommands(item).FirstOrDefault(c => c.DisplayName == CommandName);
                if (command == null)
                {
                    WriteError(new ErrorRecord(
                        new WorkflowException("Command not present or no execution rights: " + CommandName),
                        "sitecore_workflow_command_missing",
                        ErrorCategory.ObjectNotFound, null));
                    return;
                }

                WorkflowResult workflowResult = workflow.Execute(command.CommandID, item, Comments, false, new object[0]);
                if (!workflowResult.Succeeded)
                {
                    string message = workflowResult.Message;
                    if (string.IsNullOrEmpty(message))
                    {
                        message = "IWorkflow.Execute() failed for unknown reason.";
                    }
                    WriteError(new ErrorRecord(
                        new WorkflowException(message),
                        "sitecore_workflow_execution_error",
                        ErrorCategory.OperationStopped, null));
                }
            }
            catch (WorkflowStateMissingException)
            {
                WriteError(new ErrorRecord(
                    new WorkflowStateMissingException("Item workflow state does not specify the next step."),
                    "sitecore_workflow_execution_error",
                    ErrorCategory.OperationStopped, null));
            }
        }
    }
}