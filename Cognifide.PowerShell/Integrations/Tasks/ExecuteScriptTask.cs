using System;
using System.Linq;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Data.Items;
using Sitecore.Tasks;

namespace Cognifide.PowerShell.Integrations.Tasks
{
    public class ExecuteScriptTask
    {
        public void Update(Item[] items, CommandItem command, ScheduleItem schedule)
        {
            using (var session = ScriptSessionManager.NewSession(ApplicationNames.Default, true))
            {
                foreach (var scriptItem in items.Where(si => si.IsPowerShellScript() && !string.IsNullOrWhiteSpace(si[Templates.Script.Fields.ScriptBody])))
                {
                    if (!RulesUtils.EvaluateRules(scriptItem[Templates.Script.Fields.EnableRule], scriptItem)) continue;

                    session.SetItemLocationContext(scriptItem);
                    session.ExecuteScriptPart(scriptItem, true);
                }
            }
        }
    }
}