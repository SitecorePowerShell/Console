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
                var error = $"The script session with Id '{session.ID}' cannot be unloaded because it is in the Busy state. Use Stop-ScriptSession or wait for the operation to complete.";
                WriteError(new ErrorRecord(new CmdletInvocationException(error), error, ErrorCategory.ResourceBusy, session.ID));
                return;
            }

            ScriptSessionManager.RemoveSession(Session);
        }
    }
}