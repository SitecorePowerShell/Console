using System;
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
            if (session.State != RunspaceAvailability.Busy) { return; }

            if (session.ID != CurrentSessionId)
            {
                session.Abort();
            }
            else
            {
                var error = $"The current script session with Id '{session.ID}' cannot be stopped.";
                WriteError(new ErrorRecord(new CmdletInvocationException(error), error, ErrorCategory.ResourceBusy, session.ID ?? String.Empty));
            }
        }
    }
}