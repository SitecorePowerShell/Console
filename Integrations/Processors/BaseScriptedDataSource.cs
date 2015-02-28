using System;
using System.Collections.Generic;
using System.Linq;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Settings;
using Sitecore.Collections;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Text;

namespace Cognifide.PowerShell.Integrations.Processors
{
    public abstract class BaseScriptedDataSource
    {
        protected static bool IsScripted(string dataSource)
        {
            return dataSource != null &&
                   (dataSource.IndexOf("script:", StringComparison.OrdinalIgnoreCase) == 0 &&
                    dataSource.IndexOf(ApplicationSettings.ScriptLibraryPath, StringComparison.OrdinalIgnoreCase) > -1);
        }

        protected static string GetScriptedQueries(string sources, Item contextItem, ItemList items)
        {
            var unusedLocations = string.Empty;
            foreach (var location in new ListString(sources))
            {
                if (IsScripted(location))
                {
                    var scriptLocation = location.Replace("script:", "").Trim();
                    items.AddRange(RunEnumeration(scriptLocation, contextItem));
                }
                else
                {
                    unusedLocations += unusedLocations.Length > 0 ? "|" + location : location;
                }
            }
            return unusedLocations;
        }

        protected static IEnumerable<Item> RunEnumeration(string scriptSource, Item item)
        {
            Assert.ArgumentNotNull(scriptSource, "scriptSource");
            Assert.ArgumentNotNull(item, "item");
            scriptSource = scriptSource.Replace("script:", "").Trim();
            var scriptItem = item.Database.GetItem(scriptSource);
            using (var session = ScriptSessionManager.NewSession(ApplicationNames.Default, true))
            {
                var script = (scriptItem.Fields[ScriptItemFieldNames.Script] != null)
                    ? scriptItem.Fields[ScriptItemFieldNames.Script].Value
                    : string.Empty;
                script = string.Format("{0}\n{1}", ScriptSession.GetDataContextSwitch(item), script);
                return session.ExecuteScriptPart(script, false).Cast<Item>();
            }
        }
    }
}