using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Data.Items;
using Sitecore.Exceptions;

namespace Cognifide.PowerShell.Commandlets.Workflows
{
    [Cmdlet(VerbsLifecycle.Invoke, "Workflow", SupportsShouldProcess = true)]
    public class InvokeWorkflowCommand : BaseItemCommand
    {
        [Parameter]
        public string CommandName { get; set; }

        [Parameter]
        public string Comments { get; set; }

        protected override void ProcessItem(Item item)
        {
            var workflowProvider = item.Database.WorkflowProvider;
            if (workflowProvider == null)
            {
                WriteError(
                    new ErrorRecord(new WorkflowException("Workflow provider could not be obtained for database: " +
                                                          item.Database.Name),
                        "sitecore_workflow_provider_missing",
                        ErrorCategory.ObjectNotFound, null));
                return;
            }

            var workflow = workflowProvider.GetWorkflow(item);
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
                var command = workflow.GetCommands(item).FirstOrDefault(c => c.DisplayName == CommandName);
                if (command == null)
                {
                    WriteError(new ErrorRecord(
                        new WorkflowException("Command not present or no execution rights: " + CommandName),
                        "sitecore_workflow_command_missing",
                        ErrorCategory.ObjectNotFound, null));
                    return;
                }

                if (ShouldProcess(item.GetProviderPath(),
                    string.Format("Invoke command '{0}' in workflow '{1}'", command.DisplayName,
                        workflow.Appearance.DisplayName)))
                {
                    var workflowResult = workflow.Execute(command.CommandID, item, Comments, false);
                    if (!workflowResult.Succeeded)
                    {
                        var message = workflowResult.Message;
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