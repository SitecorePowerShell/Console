using System;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Settings;
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
                foreach (var item in items)
                {
                    var script = item[ScriptItemFieldNames.Script];
                    if (!String.IsNullOrEmpty(script))
                    {
                        session.SetExecutedScript(item);
                        session.SetItemLocationContext(item);
                        session.ExecuteScriptPart(script);
                    }
                }
            }
        }
    }
}