using System;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Extensions;
using Cognifide.PowerShell.PowerShellIntegrations;
using Cognifide.PowerShell.PowerShellIntegrations.Host;

namespace Cognifide.PowerShell.Commandlets.Session
{
    [Cmdlet("Get", "ScriptSession",DefaultParameterSetName = "All")]
    public class GetScriptSession : BaseCommand
    {
        [Parameter(ParameterSetName = "By ID", Mandatory = true)]
        public string Id { get; set; }

        [Parameter(ParameterSetName = "Current session", Mandatory = true)]
        public SwitchParameter Current { get; set; }

        [Parameter(ParameterSetName = "By Type", Mandatory = true)]
        public string[] Type { get; set; }

        protected override void ProcessRecord()
        {
            if (Current.IsPresent)
            {
                var scriptingHostPrivateData = Host.PrivateData.BaseObject() as ScriptingHostPrivateData;
                if (scriptingHostPrivateData == null) return;

                var id = scriptingHostPrivateData.SessionId;
                if (!string.IsNullOrEmpty(id))
                {
                    WriteObject(
                        ScriptSessionManager.GetAll().Where(s => id.Equals(s.ID, StringComparison.OrdinalIgnoreCase)),
                        true);
                }
            }
            else if (!string.IsNullOrEmpty(Id))
            {
                WriteObject(ScriptSessionManager.GetSession(Id));
            }
            else if (Type != null && Type.Length > 0)
            {
                foreach (var type in Type)
                {
                    WildcardWrite(type, ScriptSessionManager.GetAll(), session => session.ApplianceType);
                }
            }
            else
            {
                WriteObject(ScriptSessionManager.GetAll(), true);
            }
        }
    }
}