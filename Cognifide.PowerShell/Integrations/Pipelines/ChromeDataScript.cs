using System;
using System.Linq;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Modules;
using Cognifide.PowerShell.Core.Settings;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.GetChromeData;

namespace Cognifide.PowerShell.Integrations.Pipelines
{
    public class ChromeDataScript
    {
        public string IntegrationPoint => IntegrationPoints.ExperienceEditorChromeDataFeature;

        public void Process(GetChromeDataArgs args)
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
                            Log.Error(ex.Message, this);
                        }
                    }
                }
            }
        }
    }
}