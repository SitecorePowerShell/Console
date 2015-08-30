using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Cognifide.PowerShell.Core.Host;

namespace Cognifide.PowerShell.Commandlets.ScriptSessions
{
    [Cmdlet(VerbsCommon.Remove, "ScriptSession", DefaultParameterSetName = "All", SupportsShouldProcess = true)]
    public class RemoveScriptSessionCommand : BaseScriptSessionCommand
    {
        protected override void ProcessSession(ScriptSession session)
        {
            if (!ShouldProcess(session.ID, "Remove existing script session")) return;

            if (session.State == RunspaceAvailability.Busy)
            {
                WriteError(typeof (CmdletInvocationException),
                    $"The script session with Id '{session.ID}' cannot be unloaded because it is in the Busy state. Use Stop-ScriptSession or wait for the operation to complete.",
                    ErrorIds.ScriptSessionBusy, ErrorCategory.ResourceBusy, session.ID);
                return;
            }

            ScriptSessionManager.RemoveSession(session);
        }
    }
}