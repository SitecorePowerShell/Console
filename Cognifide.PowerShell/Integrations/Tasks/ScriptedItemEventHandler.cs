using System;
using System.Collections.Generic;
using System.Linq;
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

            Func<Item, bool> filter = si => si.IsPowerShellScript()
                                            && !string.IsNullOrWhiteSpace(si[Templates.Script.Fields.ScriptBody])
                                            && RulesUtils.EvaluateRules(si[Templates.Script.Fields.EnableRule], item);

            foreach (var root in ModuleManager.GetFeatureRoots(IntegrationPoints.EventHandlersFeature))
            {
                var libraryItem = root.Paths.GetSubItem(eventName);

                var applicableScriptItems = libraryItem?.Children?.Where(filter).ToArray();
                if (applicableScriptItems == null || !applicableScriptItems.Any())
                {
                    return;
                }

                using (var session = ScriptSessionManager.NewSession(ApplicationNames.Default, true))
                {
                    session.SetVariable("eventArgs", args);

                    if (item != null)
                    {
                        session.SetItemLocationContext(item);
                    }
                    
                    foreach (var scriptItem in applicableScriptItems)
                    {
                        session.ExecuteScriptPart(scriptItem, true);
                    }
                }
            }
        }

        private static string EventArgToEventName(EventArgs args)
        {
            var eventName = string.Empty;

            switch (args)
            {
                case SitecoreEventArgs _:
                    var scevent = (SitecoreEventArgs)args;
                    eventName = scevent.EventName;
                    break;
                case IPassNativeEventArgs _:
                    EventTypeMapping.TryGetValue(args.GetType(), out eventName);
                    break;
            }

            return eventName?.Replace(':', '/');
        }
    }
}