using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using Cognifide.PowerShell.Core.Host;
using Sitecore.ContentSearch.Utilities;
using Sitecore.Shell.Applications.Layouts.IDE.Wizards.NewMethodRenderingWizard;

namespace Cognifide.PowerShell.Commandlets.ScriptSessions
{
    [Cmdlet(VerbsLifecycle.Wait, "ScriptSession", SupportsShouldProcess = true)]
    [OutputType(typeof (object))]
    public class WaitScriptSessionCommand : BaseScriptSessionCommand
    {
        [Parameter]
        public int Timeout { get; set; } = -1;

        [Parameter]
        public SwitchParameter Any { get; set; }

        private readonly List<ScriptSession> sessions = new List<ScriptSession>();

        protected override void ProcessSession(ScriptSession session)
        {
            sessions.Add(session);
        }

        protected override void EndProcessing()
        {

            if (!ShouldProcess(
                sessions.Select(session => session.ID).Aggregate((seed, cur) => seed + ", " + cur),
                "Wait for running script session")) return;

            DateTime stopDateTime = Timeout > -1 ? DateTime.Now.AddSeconds(Timeout) : DateTime.MaxValue;
            while (DateTime.Now < stopDateTime)
            {
                var hasBusySessions = false;
                foreach (var session in sessions)
                {
                    if (session.State == RunspaceAvailability.Busy)
                    {
                        hasBusySessions = true;
                    }
                    else if (Any)
                    {
                        CollectFinishedSessions();
                        return;
                    }
                }
                if (!hasBusySessions)
                {
                    CollectFinishedSessions();
                    break;
                }
                Thread.Sleep(100);
            }
        }

        private void CollectFinishedSessions()
        {
            sessions.Where(session => session.State != RunspaceAvailability.Busy).ForEach(WriteObject);
        }
    }
}