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
        public virtual string Id { get; set; }

        [Parameter(ParameterSetName = "Session", Mandatory = true, ValueFromPipeline = true)]
        public virtual ScriptSession Session { get; set; }

        protected abstract void ProcessSession(ScriptSession session);

        protected override void ProcessRecord()
        {
            if (!string.IsNullOrEmpty(Id))
            {
                if (ScriptSessionManager.SessionExistsForAnyUserSession(Id))
                {
                    ScriptSessionManager.GetMatchingSessionsForAnyUserSession(Id).ForEach(ProcessSession);
                    return;
                }

                WriteError(
                    new ErrorRecord(
                        new ObjectNotFoundException($"Session with Id '{Id}' cannot be found."),
                        "sitecore_script_session_not_found", ErrorCategory.ResourceBusy, Id));
                return;
            }

            if (Session == null)
            {
                WriteError(
                    new ErrorRecord(
                        new ObjectNotFoundException("The session does not exist"),
                        "sitecore_script_session_not_found", ErrorCategory.ObjectNotFound, Id ?? string.Empty));
                return;
            }
            ProcessSession(Session);
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