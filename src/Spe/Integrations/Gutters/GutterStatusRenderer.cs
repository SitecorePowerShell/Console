using System;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Shell.Applications.ContentEditor.Gutters;
using Spe.Core.Diagnostics;
using Spe.Core.Extensions;
using Spe.Core.Host;
using Spe.Core.Modules;
using Spe.Core.Settings;
using Spe.Core.Utility;

namespace Spe.Integrations.Gutters
{
    // Inherit from the GutterRenderer in order to override the GetIconDescriptor.
    public class GutterStatusRenderer : GutterRenderer
    {
        // We override the GetIconDescriptor so a script can be called in it's place.
        protected override GutterIconDescriptor GetIconDescriptor(Item item)
        {
            return SpeTimer.Measure("gutter script execution", true, () =>
            {
                // The scriptId parameter is configured when we create a new gutter
                // here /sitecore/content/Applications/Content Editor/Gutters
                if (!Parameters.ContainsKey("scriptId")) return null;

                var scriptId = new ID(Parameters["scriptId"]);
                var scriptDb = string.IsNullOrEmpty(Parameters["scriptDb"])
                    ? ApplicationSettings.ScriptLibraryDb
                    : Parameters["scriptId"];

                var db = Factory.GetDatabase(scriptDb);
                var scriptItem = db.GetItem(scriptId);

                // If a script is configured but does not exist or is of a wrong template then do nothing.
                if (scriptItem == null || !scriptItem.IsPowerShellScript() ||
                    string.IsNullOrWhiteSpace(scriptItem[Templates.Script.Fields.ScriptBody]) ||
                    !RulesUtils.EvaluateRules(scriptItem[Templates.Script.Fields.EnableRule], item)) return null;

                var featureRoot = ModuleManager.GetItemModule(scriptItem)?
                    .GetFeatureRoot(IntegrationPoints.ContentEditorGuttersFeature);
                if (!RulesUtils.EvaluateRules(featureRoot?[Templates.ScriptLibrary.Fields.EnableRule], item)) return null;

                try
                {
                    // Create a new session for running the script.
                    var session = ScriptSessionManager.GetSession(scriptItem[Templates.Script.Fields.PersistentSessionId],
                        IntegrationPoints.ContentEditorGuttersFeature);

                    // We will need the item variable in the script.
                    session.SetItemLocationContext(item);

                    // Any objects written to the pipeline in the script will be returned.
                    var output = session.ExecuteScriptPart(scriptItem, false);
                    foreach (var result in output)
                    {
                        if (result.GetType() == typeof (GutterIconDescriptor))
                        {
                            return (GutterIconDescriptor) result;
                        }
                    }
                }
                catch (Exception ex)
                {
                    PowerShellLog.Error($"Error while invoking script '{scriptItem?.Paths.Path}' for rendering in Content Editor gutter.", ex);
                }
                return null;
            });
        }
    }
}