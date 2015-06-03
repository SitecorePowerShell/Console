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
    public class ContentEditorWarningScript// : PipelineProcessor<GetContentEditorWarningsArgs>
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
                        session.SetVariable("args", args);

                        try
                        {
                            session.SetExecutedScript(scriptItem);
                            var output = session.ExecuteScriptPart(script, false);
                            foreach (var result in output)
                            {
                                if (result is GetContentEditorWarningsArgs.ContentEditorWarning)
                                {
                                    var warning = result as GetContentEditorWarningsArgs.ContentEditorWarning;
                                    var addedWarning = args.Add();
                                    addedWarning.Title = warning.Title;
                                    addedWarning.Text = warning.Text;
                                    addedWarning.Icon = warning.Icon;
                                    addedWarning.HideFields = warning.HideFields;
                                    addedWarning.IsExclusive = warning.IsExclusive;
                                    addedWarning.IsFullscreen = warning.IsFullscreen;
                                    addedWarning.Key = warning.Key;

                                    foreach (var option in warning.Options)
                                    {
                                        addedWarning.AddOption(option.Part1, option.Part2);
                                    }
                                }
                            }
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