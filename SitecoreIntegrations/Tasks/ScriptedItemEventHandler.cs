using System;
using Cognifide.PowerShell.PowerShellIntegrations;
using Cognifide.PowerShell.PowerShellIntegrations.Host;
using Cognifide.PowerShell.PowerShellIntegrations.Modules;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Events;

namespace Cognifide.PowerShell.SitecoreIntegrations.Tasks
{
    public class ScriptedItemEventHandler
    {
        public void OnEvent(object sender, EventArgs args)
        {
            var eventArgs = args as SitecoreEventArgs;
            if (eventArgs == null)
            {
                return;
            }
            var item = eventArgs.Parameters[0] as Item;
            string eventName = eventArgs.EventName.Replace(':', '/');

            foreach (var root in ModuleManager.GetFeatureRoots(IntegrationPoints.EventHandlersFeature))
            {
                Item libraryItem = root.Paths.GetSubItem(eventName);

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
}