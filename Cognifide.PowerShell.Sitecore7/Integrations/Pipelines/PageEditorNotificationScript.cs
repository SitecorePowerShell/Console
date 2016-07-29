using System;
using System.Linq;
using Cognifide.PowerShell.Core.Diagnostics;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Modules;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.GetPageEditorNotifications;

namespace Cognifide.PowerShell.VersionSpecific.Integrations.Pipelines
{
    public class PageEditorNotificationScript
    {
        private string IntegrationPoint => IntegrationPoints.PageEditorNotificationFeature;

        public void Process(GetPageEditorNotificationsArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            foreach (var libraryItem in ModuleManager.GetFeatureRoots(IntegrationPoint))
            {
                if (!libraryItem.HasChildren) return;

                foreach (var scriptItem in libraryItem.Children.ToList())
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