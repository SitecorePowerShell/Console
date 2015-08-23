using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Cognifide.PowerShell.Core.Host;

namespace Cognifide.PowerShell.Commandlets.ScriptSessions
{
    [Cmdlet(VerbsCommunications.Receive, "ScriptSession", DefaultParameterSetName = "All")]
    public class ReceiveScriptSessionCommand : BaseScriptSessionCommand
    {
        [Parameter]
        public virtual SwitchParameter KeepResult { get; set; }

        [Parameter]
        public virtual SwitchParameter KeepSession { get; set; }

        protected override void ProcessSession(ScriptSession session)
        {
            if (session.State == RunspaceAvailability.Busy)
            {
                var error = $"The script session with Id '{session.ID}' cannot be received because it is in the Busy state. Use Stop-ScriptSession or wait for the operation to complete.";
                WriteError(new ErrorRecord(new CmdletInvocationException(error), error, ErrorCategory.ResourceBusy, session.ID));
                return;
            }

            WriteObject(session.AsyncResultsStore);

            if (KeepResult) return;
            session.AsyncResultsStore = null;

            if (KeepSession) return;
            ScriptSessionManager.RemoveSession(session);
        }
    }
}