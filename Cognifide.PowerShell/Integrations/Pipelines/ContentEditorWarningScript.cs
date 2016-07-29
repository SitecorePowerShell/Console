using System;
using System.Linq;
using Cognifide.PowerShell.Core.Diagnostics;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Modules;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.GetContentEditorWarnings;

namespace Cognifide.PowerShell.Integrations.Pipelines
{
    public class ContentEditorWarningScript
    {
        public string IntegrationPoint
        {
            get { return IntegrationPoints.ContentEditorWarningFeature; }
        }

        public void Process(GetContentEditorWarningsArgs args)
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
                            session.SetItemLocationContext(args.Item);
                            session.ExecuteScriptPart(scriptItem, false);
                        }
                        catch (Exception ex)
                        {
                            PowerShellLog.Error($"Error while invoking script '{scriptItem?.Paths.Path}' in Content Editor Warning pipeline.", ex);
                        }
                    }
                }
            }
        }
    }
}