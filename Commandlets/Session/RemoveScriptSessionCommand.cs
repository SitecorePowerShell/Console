using System;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Host;

namespace Cognifide.PowerShell.Commandlets.Session
{
    [Cmdlet(VerbsCommon.Remove, "ScriptSession", DefaultParameterSetName = "All")]
    public class RemoveScriptSession : BaseCommand
    {
        [Parameter(ParameterSetName = "ID", Mandatory = true, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public string Id { get; set; }

        [Parameter(ParameterSetName = "Session", Mandatory = true, ValueFromPipeline = true)]
        public ScriptSession Session { get; set; }

        protected override void ProcessRecord()
        {
            // Prevent closing out the current session.
            var scriptingHostPrivateData = Host.PrivateData.BaseObject() as ScriptingHostPrivateData;
            if (scriptingHostPrivateData == null) return;

            var id = scriptingHostPrivateData.SessionId;
            if (String.IsNullOrEmpty(id))
            {
                return;
            }

            if (!String.IsNullOrEmpty(Id) && Id != id)
            {
                ScriptSessionManager.RemoveSession(Id);
            }
            else if (Session != null && Session.ID != id)
            {
                ScriptSessionManager.RemoveSession(Session);
            }
        }
    }
}