using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Cognifide.PowerShell.Core.Host;

namespace Cognifide.PowerShell.Commandlets.ScriptSessions
{
    [Cmdlet(VerbsCommunications.Receive, "ScriptSession", DefaultParameterSetName = "All", SupportsShouldProcess = true)]
    public class ReceiveScriptSessionCommand : BaseScriptSessionCommand
    {
        [Parameter]
        public virtual SwitchParameter KeepResult { get; set; }

        [Parameter]
        public virtual SwitchParameter KeepSession { get; set; }

        [Parameter]
        public virtual SwitchParameter HostOutput { get; set; }

        protected override void ProcessSession(ScriptSession session)
        {
            if (!ShouldProcess(session.ID, "Receive results from existing script session")) return;

            if (session.State == RunspaceAvailability.Busy)
            {
                WriteError(typeof (CmdletInvocationException),
                    $"The script session with Id '{session.ID}' cannot be received because it is in the Busy state. Use Stop-ScriptSession or wait for the operation to complete.",
                    ErrorIds.ScriptSessionBusy,
                    ErrorCategory.ResourceBusy, session.ID);
                return;
            }

            if (HostOutput)
            {
                WriteObject(session.Output.ToString());
            }
            else
            {
                WriteObject(session.JobResultsStore);
            }

            if (KeepResult) return;
            session.JobResultsStore = null;

            if (KeepSession) return;
            ScriptSessionManager.RemoveSession(session);
        }
    }
}