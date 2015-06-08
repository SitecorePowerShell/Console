using System;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Modules;
using Cognifide.PowerShell.Core.Settings;
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
                        var script = (scriptItem.Fields[ScriptItemFieldNames.Script] != null)
                            ? scriptItem.Fields[ScriptItemFieldNames.Script].Value
                            : String.Empty;
                        session.SetVariable("pipelineArgs", args);

                        try
                        {
                            session.SetExecutedScript(scriptItem);
                            session.SetItemLocationContext(args.Item);
                            session.ExecuteScriptPart(script, false);
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