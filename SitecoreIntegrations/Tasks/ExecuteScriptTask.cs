using System;
using Cognifide.PowerShell.PowerShellIntegrations.Host;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
using Sitecore.Data.Items;
using Sitecore.Tasks;

namespace Cognifide.PowerShell.SitecoreIntegrations.Tasks
{
    public class ExecuteScriptTask
    {
        public void Update(Item[] items, CommandItem command, ScheduleItem schedule)
        {
            using (var session = new ScriptSession(ApplicationNames.Default))
            {
                foreach (var item in items)
                {
                    string script = item["Script"];
                    if (!String.IsNullOrEmpty(script))
                    {
                        session.SetExecutedScript(item);
                        session.SetContextItem(item);
                        session.SetItemLocationContext(item);
                        session.ExecuteScriptPart(script);
                    }
                }
            }
        }
    }
}