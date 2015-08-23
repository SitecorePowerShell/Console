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
                WriteError(
                    new ErrorRecord(
                        new CmdletInvocationException(
                            "The session cannot be received from as it is Busy. Stop-ScriptSession or wait for the operation to end before attempting to receive from it again."),
                        "sitecore_cannot_receive_script_session", ErrorCategory.ResourceBusy, session.ID));
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