using System;
using Cognifide.PowerShell.PowerShellIntegrations;
using Cognifide.PowerShell.PowerShellIntegrations.Host;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Events;

namespace Cognifide.PowerShell.SitecoreIntegrations.Tasks
{
    public class ScriptedItemEventHandler
    {
        private const string EventHandlerLibraryPath =
            ScriptLibrary.Path + "Event Handlers/";

        public void OnEvent(object sender, EventArgs args)
        {
            var eventArgs = args as SitecoreEventArgs;
            if (eventArgs == null)
            {
                return;
            }
            var item = eventArgs.Parameters[0] as Item;
            string eventName = eventArgs.EventName.Replace(':', '/');

            Database database = item != null ? item.Database ?? Context.ContentDatabase : Context.ContentDatabase;
            Item libraryItem = database.GetItem(EventHandlerLibraryPath + eventName);

            if (libraryItem == null)
            {
                return;
            }
            using (var session = new ScriptSession(ApplicationNames.Default))
            {
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
}