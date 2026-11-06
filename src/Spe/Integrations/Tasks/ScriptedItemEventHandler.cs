using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore;
using Sitecore.Data.Events;
using Sitecore.Data.Items;
using Sitecore.Events;
using Sitecore.Rules;
using Sitecore.Security.Accounts;
using Sitecore.SecurityModel;
using Spe.Core.Diagnostics;
using Spe.Core.Extensions;
using Spe.Core.Host;
using Spe.Core.Modules;
using Spe.Core.Settings;
using Spe.Core.Utility;

namespace Spe.Integrations.Tasks
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
            if (EventDisabler.IsActive) return;

            Item item = null;
            var eventName = EventArgToEventName(args);
            if (args is SitecoreEventArgs scevent)
            {
                item = scevent.Parameters[0] as Item;
            }

            if (string.IsNullOrEmpty(eventName))
            {
                return;
            }

            RuleContext GetRuleContext(Item contextItem, Item scriptItem)
            {
                var ruleContext = new RuleContext
                {
                    Item = contextItem ?? scriptItem
                };
                ruleContext.Parameters.Add("ScriptItem", scriptItem);

                return ruleContext;
            }

            Func<Item, bool> filter = si => si.IsPowerShellScript()
                                            && !string.IsNullOrWhiteSpace(si[Templates.Script.Fields.ScriptBody])
                                            && RulesUtils.EvaluateRules(si[Templates.Script.Fields.EnableRule], GetRuleContext(item, si));

            foreach (var root in ModuleManager.GetFeatureRoots(IntegrationPoints.EventHandlersFeature))
            {
                if (!RulesUtils.EvaluateRules(root?[Templates.ScriptLibrary.Fields.EnableRule], GetRuleContext(item, root))) continue;

                var libraryItem = root?.Paths.GetSubItem(eventName);

                var applicableScriptItems = libraryItem?.Children?.Where(filter).ToArray();
                if (applicableScriptItems == null || !applicableScriptItems.Any())
                {
                    continue;
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
                        if (DelegatedAccessManager.IsElevated(Context.User, scriptItem))
                        {
                            var jobUser = DelegatedAccessManager.GetDelegatedUser(Context.User, scriptItem);
                            PowerShellLog.Audit($"[DelegatedAccess] action=executing impersonatedUser={jobUser.Name} script=\"{scriptItem.Name} {scriptItem.ID}\" source=EventHandler");
                            using (new UserSwitcher(jobUser))
                            {
                                session.ExecuteScriptPart(scriptItem, true);
                            }
                        }
                        else
                        {
                            session.ExecuteScriptPart(scriptItem, true);
                        }
                    }
                }
            }
        }

        private static string EventArgToEventName(EventArgs args)
        {
            var eventName = string.Empty;

            switch (args)
            {
                case SitecoreEventArgs scevent:
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