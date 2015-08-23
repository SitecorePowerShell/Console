using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Cognifide.PowerShell.Core.Host;

namespace Cognifide.PowerShell.Commandlets.ScriptSessions
{
    [Cmdlet(VerbsCommon.Remove, "ScriptSession", DefaultParameterSetName = "All")]
    public class RemoveScriptSessionCommand : BaseScriptSessionCommand
    {
        protected override void ProcessSession(ScriptSession session)
        {
            if (session.State == RunspaceAvailability.Busy)
            {
                WriteError(
                    new ErrorRecord(
                        new CmdletInvocationException(
                            "The session cannot be unloaded as it is currently Busy. Stop-ScriptSession or wait for the operation to end before attempting to unload it again."),
                        "sitecore_cannot_remove_script_session", ErrorCategory.ResourceBusy, session.ID));
                return;
            }

            ScriptSessionManager.RemoveSession(Session);
        }
    }
}