using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Cognifide.PowerShell.PowerShellIntegrations.Host;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Events;

namespace Cognifide.PowerShell.SitecoreIntegrations.Tasks
{
    public class LibraryWatcher
    {
        private const string EventHandlerLibraryPath = "/sitecore/system/Modules/PowerShell/Script Library/";

        public void OnEvent(object sender, EventArgs args)
        {
            var eventArgs = args as SitecoreEventArgs;
            if (eventArgs == null)
            {
                return;
            }
            Item item = eventArgs.Parameters[0] as Item;
            string eventName = eventArgs.EventName.Replace(':', '/');

            Database database = item != null ? item.Database ?? Context.ContentDatabase : Context.ContentDatabase;
            Item libraryItem = database.GetItem(EventHandlerLibraryPath + eventName);

            if (libraryItem == null)
            {
                return;
            }
            var session = new ScriptSession(ApplicationNames.Default);

            foreach (Item scriptItem in libraryItem.Children)
            {
                if (item != null)
                {
                    session.SetItemLocationContext(item);
                }

                session.SetVariable("eventArgs", eventArgs);
                string script = scriptItem["Script"];
                if (!String.IsNullOrEmpty(script))
                {
                    session.ExecuteScriptPart(script);
                }
            }
        }
    }
}