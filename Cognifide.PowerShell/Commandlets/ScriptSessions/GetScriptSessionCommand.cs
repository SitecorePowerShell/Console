using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.ContentSearch.Utilities;

namespace Cognifide.PowerShell.Commandlets.ScriptSessions
{
    [Cmdlet(VerbsCommon.Get, "ScriptSession", DefaultParameterSetName = "All")]
    public class GetScriptSessionCommand : BaseScriptSessionCommand
    {
        [Parameter(ParameterSetName = "Current", Mandatory = true)]
        public SwitchParameter Current { get; set; }

        [Parameter]
        [Alias("Type")]
        public string[] SessionType { get; set; }

        [Parameter]
        public RunspaceAvailability State { get; set; }

        private List<WildcardPattern> patterns;

        protected override void ProcessSession(ScriptSession session)
        {
            if (State != RunspaceAvailability.None && State != session.State)
            {
                return;
            }

            if (SessionType != null && SessionType.Length > 0)
            {
                if (patterns == null)
                {
                    patterns = new List<WildcardPattern>(SessionType.Length);
                    foreach (var type in SessionType)
                    {
                        patterns.Add(WildcardUtils.GetWildcardPattern(type));
                    }
                }
                if (!patterns.Any(pattern => pattern.IsMatch(session.ApplianceType)))
                {
                    return;
                }
            }

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
                    WriteObject(ScriptSessionManager.GetAll().Where(s => currentSessionId.Equals(s.ID, StringComparison.OrdinalIgnoreCase)), true);
                }
            }
            else if (Id != null && Id.Length > 0)
            {
                foreach (var id in Id)
                {
                    if (ScriptSessionManager.SessionExistsForAnyUserSession(id))
                    {
                        ScriptSessionManager.GetMatchingSessionsForAnyUserSession(id).ForEach(ProcessSession);
                    }
                    else
                    {
                        WriteError(typeof (ObjectNotFoundException),
                            $"The script session with with Id '{id}' cannot be found.", ErrorIds.ScriptSessionNotFound,
                            ErrorCategory.ResourceBusy, Id);
                    }
                }
            }
            else
            {
                ScriptSessionManager.GetAll().ForEach(ProcessSession);
            }
        }
    }
}