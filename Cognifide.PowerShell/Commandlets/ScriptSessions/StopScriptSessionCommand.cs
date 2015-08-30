using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Cognifide.PowerShell.Core.Host;

namespace Cognifide.PowerShell.Commandlets.ScriptSessions
{
    [Cmdlet(VerbsLifecycle.Stop, "ScriptSession", SupportsShouldProcess = true)]
    public class StopScriptSessionCommand : BaseScriptSessionCommand
    {
        protected override void ProcessSession(ScriptSession session)
        {
            if (session.State != RunspaceAvailability.Busy) { return; }

            if (!ShouldProcess(session.ID, "Abort running script session")) return;

            if (session.ID != CurrentSessionId)
            {
                session.Abort();
            }
            else
            {
                WriteError(typeof (CmdletInvocationException), $"The current script session with Id '{session.ID}' cannot be stopped.", 
                    ErrorIds.ScriptSessionCannotBeStopped, ErrorCategory.ResourceBusy,session.ID ?? string.Empty);
            }

            WriteObject(session);
        }
    }
}