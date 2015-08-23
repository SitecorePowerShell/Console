using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Cognifide.PowerShell.Core.Host;

namespace Cognifide.PowerShell.Commandlets.ScriptSessions
{
    [Cmdlet(VerbsLifecycle.Stop, "ScriptSession", SupportsShouldProcess = true)]
    [OutputType(typeof (object))]
    public class StopScriptSessionCommand : BaseScriptSessionCommand
    {
        protected override void ProcessSession(ScriptSession session)
        {
            if (session.State == RunspaceAvailability.Busy)
            {
                if (session.ID != CurrentSessionId)
                {
                    session.Abort();
                }
                else
                {
                    WriteError(
                        new ErrorRecord(
                            new CmdletInvocationException("Current session cannot be stopped."),
                            "sitecore_cannot_stop_current_script_session", ErrorCategory.ResourceBusy, session.ID ?? string.Empty));
                }
            }
            else
            {
                WriteError(
                    new ErrorRecord(
                        new CmdletInvocationException("The session does not exist or it is not busy."),
                        "sitecore_cannot_stop_script_session", ErrorCategory.ResourceBusy, session.ID??string.Empty));
            }
        }

    }
}