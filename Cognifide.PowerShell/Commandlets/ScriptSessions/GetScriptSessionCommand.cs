using System;
using System.Data;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Host;

namespace Cognifide.PowerShell.Commandlets.ScriptSessions
{
    [Cmdlet(VerbsCommon.Get, "ScriptSession", DefaultParameterSetName = "All")]
    public class GetScriptSessionCommand : BaseScriptSessionCommand
    {
        [Parameter(ParameterSetName = "Current", Mandatory = true)]
        public SwitchParameter Current { get; set; }

        [Parameter(ParameterSetName = "Type", Mandatory = true)]
        public string[] Type { get; set; }

        protected override void ProcessSession(ScriptSession session)
        {
            WriteObject(session);
        }

        protected override void ProcessRecord()
        {
            if (ParameterSetName.Is("ID") || ParameterSetName.Is("Session"))
            {
                base.ProcessRecord();
                return;
            }

            if (Current.IsPresent)
            {
                var currentSessionId = CurrentSessionId;
                if (!string.IsNullOrEmpty(currentSessionId))
                {
                    WriteObject(
                        ScriptSessionManager.GetAll().Where(s => currentSessionId.Equals(s.ID, StringComparison.OrdinalIgnoreCase)),
                        true);
                }
            }
            else if (!string.IsNullOrEmpty(Id))
            {
                if (ScriptSessionManager.SessionExistsForAnyUserSession(Id))
                {
                    WriteObject(ScriptSessionManager.GetMatchingSessionsForAnyUserSession(Id),true);
                }
                else
                {
                    WriteError(
                        new ErrorRecord(
                            new ObjectNotFoundException($"Session with Id '{Id}' cannot be found."),
                            "sitecore_script_session_not_found", ErrorCategory.ResourceBusy, Id));

                }
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