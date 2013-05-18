using System;
using Cognifide.PowerShell.Shell.Host;
using Cognifide.PowerShell.Shell.Settings;
using Sitecore.Data.Items;
using Sitecore.Tasks;

namespace Cognifide.PowerShell.SitecoreIntegrations.Tasks
{
    public class ExecuteScriptTask
    {
        public void Update(Item[] items, CommandItem command, ScheduleItem schedule)
        {
            var session = new ScriptSession(ApplicationNames.Default);
            foreach (Item item in items)
            {
                string script = item["Script"];
                if (!String.IsNullOrEmpty(script))
                {
                    session.ExecuteScriptPart(script);
                }
            }
        }
    }
}