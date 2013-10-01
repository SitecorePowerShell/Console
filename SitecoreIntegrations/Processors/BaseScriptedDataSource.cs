using System;
using System.Collections.Generic;
using System.Linq;
using Cognifide.PowerShell.PowerShellIntegrations;
using Cognifide.PowerShell.PowerShellIntegrations.Host;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
using Sitecore.Collections;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Text;

namespace Cognifide.PowerShell.SitecoreIntegrations.Processors
{
    public abstract class BaseScriptedDataSource
    {
        protected static bool IsScripted(string dataSource)
        {
            
            return dataSource != null &&
                   (dataSource.IndexOf("script:", StringComparison.OrdinalIgnoreCase) > -1 ||
                    dataSource.IndexOf(ScriptLibrary.Path,
                                       StringComparison.OrdinalIgnoreCase) > -1);
        }

        protected static string GetScriptedQueries(string sources, Item contextItem, ItemList items)
        {
            string unusedLocations = string.Empty;
            foreach (string location in new ListString(sources))
            {
                if (IsScripted(location))
                {
                    string scriptLocation = location.Replace("script:", "").Trim();
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
            Assert.ArgumentNotNull(scriptSource,"scriptSource");
            Assert.ArgumentNotNull(item, "item");
            scriptSource = scriptSource.Replace("script:", "").Trim();
            Item scriptItem = item.Database.GetItem(scriptSource);
            using (var session = new ScriptSession(ApplicationNames.Default))
            {
                String script = (scriptItem.Fields[ScriptItemFieldNames.Script] != null)
                    ? scriptItem.Fields[ScriptItemFieldNames.Script].Value
                    : string.Empty;
                script = String.Format(
                    "cd \"{0}:{1}\"\n", item.Database.Name, item.Paths.Path.Replace("/", "\\").Substring(9)) + script;
                return session.ExecuteScriptPart(script, false).Cast<Item>();
            }
        }
    }
}