using System;
using Cognifide.PowerShell.PowerShellIntegrations.Host;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Shell.Applications.ContentEditor.Gutters;

namespace Cognifide.PowerShell.Gutters
{
    // Inherit from the GutterRenderer in order to override the GetIconDescriptor.
    public class GutterStatusRenderer : GutterRenderer
    {
        // We override the GetIconDescriptor so a script can be called in it's place.
        protected override GutterIconDescriptor GetIconDescriptor(Item item)
        {
            // The scriptId parameter is configured when we create a new gutter
            // here /sitecore/content/Applications/Content Editor/Gutters
            if (!Parameters.ContainsKey("scriptId")) return null;

            var scriptId = new ID(Parameters["scriptId"]);

            var db = Factory.GetDatabase("master");
            var scriptItem = db.GetItem(scriptId);

            // If a script is configured but does not exist then return.
            if (scriptItem == null) return null;

            // Create a new session for running the script.
            using (var session = new ScriptSession(ApplicationNames.Default))
            {
                var script = (scriptItem.Fields[ScriptItemFieldNames.Script] != null)
                    ? scriptItem.Fields[ScriptItemFieldNames.Script].Value
                    : String.Empty;

                // We will need the item variable in the script.
                session.SetVariable("item", item);

                try
                {
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
                    Log.Error(ex.Message, this);
                }
            }

            return null;
        }
    }
}