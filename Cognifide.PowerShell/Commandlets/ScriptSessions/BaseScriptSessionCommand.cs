using System.Data;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Commandlets.Interactive;
using Cognifide.PowerShell.Core.Host;
using Sitecore.ContentSearch.Utilities;

namespace Cognifide.PowerShell.Commandlets.ScriptSessions
{
    public abstract class BaseScriptSessionCommand : BaseShellCommand
    {
        [Parameter(ParameterSetName = "ID", Mandatory = true, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public virtual string[] Id { get; set; }

        [Parameter(ParameterSetName = "Session", Mandatory = true, ValueFromPipeline = true)]
        public virtual ScriptSession[] Session { get; set; }

        protected abstract void ProcessSession(ScriptSession session);

        protected override void ProcessRecord()
        {
            if (Id != null && Id.Length > 0)
            {
                foreach (var sessionId in Id)
                {
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        if (ScriptSessionManager.SessionExistsForAnyUserSession(sessionId))
                        {
                            var sessions = ScriptSessionManager.GetMatchingSessionsForAnyUserSession(sessionId).ToList();
                            foreach (var session in sessions)
                            {
                                ProcessSession(session);
                            }
                        }
                        else
                        {
                            WriteError(typeof (ObjectNotFoundException),
                                $"The script session with Id '{sessionId}' cannot be found.",
                                ErrorIds.ScriptSessionNotFound, ErrorCategory.ResourceUnavailable, sessionId);
                        }
                    }
                    else
                    {
                        WriteError(typeof(ObjectNotFoundException),
                            "The script session Id cannot be null or empty.",
                            ErrorIds.ScriptSessionNotFound, ErrorCategory.ResourceUnavailable, Id);
                    }
                }

                return;
            }

            if (Session == null || Session.Length == 0)
            {
                WriteError(typeof (ObjectNotFoundException), "Script session cannot be found.", 
                    ErrorIds.ScriptSessionNotFound, ErrorCategory.ResourceUnavailable, string.Empty);
                return;
            }

            foreach (var session in Session)
            {
                ProcessSession(session);
            }
        }

        protected string CurrentSessionId => HostData == null ? string.Empty : HostData.SessionId;
    }
}