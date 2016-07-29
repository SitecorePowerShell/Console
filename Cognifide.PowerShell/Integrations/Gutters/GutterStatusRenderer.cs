using System;
using Cognifide.PowerShell.Core.Diagnostics;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Modules;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Shell.Applications.ContentEditor.Gutters;

namespace Cognifide.PowerShell.Integrations.Gutters
{
    // Inherit from the GutterRenderer in order to override the GetIconDescriptor.
    public class GutterStatusRenderer : GutterRenderer
    {
        // We override the GetIconDescriptor so a script can be called in it's place.
        protected override GutterIconDescriptor GetIconDescriptor(Item item)
        {
            return SpeTimer.Measure("gutter script execution", () =>
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

                // If a script is configured but does not exist then return.
                if (scriptItem == null) return null;

                try
                {
                    // Create a new session for running the script.
                    var session = ScriptSessionManager.GetSession(scriptItem[ScriptItemFieldNames.PersistentSessionId],
                        IntegrationPoints.ContentEditorGuttersFeature);

                    var script = (scriptItem.Fields[ScriptItemFieldNames.Script] != null)
                        ? scriptItem.Fields[ScriptItemFieldNames.Script].Value
                        : string.Empty;

                    // We will need the item variable in the script.
                    session.SetItemLocationContext(item);

                    //let the session know which script is being executed
                    session.SetExecutedScript(scriptItem);

                    // Any objects written to the pipeline in the script will be returned.
                    var output = session.ExecuteScriptPart(script, false);
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