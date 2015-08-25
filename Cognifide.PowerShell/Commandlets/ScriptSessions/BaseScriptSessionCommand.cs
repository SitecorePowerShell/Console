using System.Data;
using System.Management.Automation;
using Cognifide.PowerShell.Commandlets.Interactive;
using Cognifide.PowerShell.Core.Extensions;
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
                foreach (var id in Id)
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        if (ScriptSessionManager.SessionExistsForAnyUserSession(id))
                        {
                            ScriptSessionManager.GetMatchingSessionsForAnyUserSession(id).ForEach(ProcessSession);
                        }
                        else
                        {
                            WriteError(typeof (ObjectNotFoundException),
                                $"The script session with Id '{Id}' cannot be found.",
                                ErrorIds.ScriptSessionNotFound, ErrorCategory.ResourceUnavailable, Id);
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

        protected string CurrentSessionId
        {
            get
            {
                var scriptingHostPrivateData = Host.PrivateData.BaseObject() as ScriptingHostPrivateData;
                if (scriptingHostPrivateData == null) return string.Empty;
                return scriptingHostPrivateData.SessionId;
            }
        }
    }
}