using System;
using System.Linq;
using Cognifide.PowerShell.Core.Diagnostics;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Modules;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.GetPageEditorNotifications;

namespace Cognifide.PowerShell.Integrations.Pipelines
{
    public class PageEditorNotificationScript
    {
        private string IntegrationPoint => IntegrationPoints.PageEditorNotificationFeature;

        public void Process(GetPageEditorNotificationsArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            Func<Item, bool> filter = si => si.IsPowerShellScript()
                                            && !string.IsNullOrWhiteSpace(si[Templates.Script.Fields.ScriptBody])
                                            && RulesUtils.EvaluateRules(si[Templates.Script.Fields.EnableRule], args.ContextItem);

            foreach (var libraryItem in ModuleManager.GetFeatureRoots(IntegrationPoint))
            {
                var applicableScriptItems = libraryItem?.Children?.Where(filter).ToArray();
                if (applicableScriptItems == null || !applicableScriptItems.Any())
                {
                    return;
                }

                foreach (var scriptItem in applicableScriptItems)
                {
                    using (var session = ScriptSessionManager.NewSession(ApplicationNames.Default, true))
                    {
                        session.SetVariable("pipelineArgs", args);

                        try
                        {
                            session.SetItemLocationContext(args.ContextItem);
                            session.ExecuteScriptPart(scriptItem, false);
                        }
                        catch (Exception ex)
                        {
                            PowerShellLog.Error($"Error while invoking script '{scriptItem?.Paths.Path}' in Page Editor Notification pipeline.", ex);
                        }
                    }
                }
            }
        }
    }
}