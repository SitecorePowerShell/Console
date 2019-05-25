using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using Spe.Core.Extensions;
using Spe.Core.Host;

namespace Spe.Commandlets.ScriptSessions
{
    [Cmdlet(VerbsLifecycle.Wait, "ScriptSession", SupportsShouldProcess = true)]
    [OutputType(typeof (object))]
    public class WaitScriptSessionCommand : BaseScriptSessionCommand
    {
        [Parameter]
        public int Timeout { get; set; } = -1;

        [Parameter]
        public SwitchParameter Any { get; set; }

        private readonly List<ScriptSession> _sessions = new List<ScriptSession>();

        protected override void ProcessSession(ScriptSession session)
        {
            _sessions.Add(session);
        }

        protected override void EndProcessing()
        {

            if (!ShouldProcess(
                _sessions.Select(session => session.ID).Aggregate((seed, cur) => seed + ", " + cur),
                "Wait for running script session")) return;

            var stopDateTime = Timeout > -1 ? DateTime.Now.AddSeconds(Timeout) : DateTime.MaxValue;
            while (DateTime.Now < stopDateTime)
            {
                var hasBusySessions = false;
                foreach (var session in _sessions.ToArray())
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
                    else
                    {
                        WriteObject(session);
                        _sessions.Remove(session);
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
            _sessions.Where(session => session.State != RunspaceAvailability.Busy).ForEach(WriteObject);
        }
    }
}