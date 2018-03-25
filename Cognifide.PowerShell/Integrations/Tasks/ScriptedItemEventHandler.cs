using System;
using System.Collections.Generic;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Modules;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Data.Events;
using Sitecore.Data.Items;
using Sitecore.Events;

namespace Cognifide.PowerShell.Integrations.Tasks
{
    public class ScriptedItemEventHandler
    {
        private static readonly Dictionary<Type, string> EventTypeMapping = new Dictionary<Type, string>
        {
            {typeof(PublishEndRemoteEventArgs), "publish:end:remote"},
            {typeof(PublishCompletedRemoteEventArgs), "publish:complete:remote"}
        };

        public void OnEvent(object sender, EventArgs args)
        {
            Item item = null;
            var eventName = EventArgToEventName(args);
            if (args is SitecoreEventArgs)
            {
                var scevent = (SitecoreEventArgs)args;
                item = scevent.Parameters[0] as Item;
            }

            if (String.IsNullOrEmpty(eventName))
            {
                return;
            }

            foreach (var root in ModuleManager.GetFeatureRoots(IntegrationPoints.EventHandlersFeature))
            {
                var libraryItem = root.Paths.GetSubItem(eventName);

                if (libraryItem == null)
                {
                    return;
                }

                using (var session = ScriptSessionManager.NewSession(ApplicationNames.Default, true))
                {
                    foreach (Item scriptItem in libraryItem.Children)
                    {
                        if (!scriptItem.IsPowerShellScript())
                        {
                            continue;
                        }

                        if (!RulesUtils.EvaluateRules(scriptItem[Templates.Script.Fields.EnableRule], item))
                        {
                            continue;
                        }

                        if (item != null)
                        {
                            session.SetItemLocationContext(item);
                        }
                        session.SetExecutedScript(scriptItem);
                        session.SetVariable("eventArgs", args);
                        var script = scriptItem[Templates.Script.Fields.ScriptBody];
                        if (!String.IsNullOrEmpty(script))
                        {
                            session.ExecuteScriptPart(script);
                        }
                    }
                }
            }
        }

        private static string EventArgToEventName(EventArgs args)
        {
            var eventName = String.Empty;

            if (args is SitecoreEventArgs)
            {
                var scevent = (SitecoreEventArgs)args;
                eventName = scevent.EventName;
            }
            else if(args is IPassNativeEventArgs)
            {
                EventTypeMapping.TryGetValue(args.GetType(), out eventName);
            }

            return eventName?.Replace(':', '/');
        }
    }
}