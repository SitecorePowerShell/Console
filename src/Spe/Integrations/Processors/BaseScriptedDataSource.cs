using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sitecore.Collections;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Text;
using Spe.Core.Extensions;
using Spe.Core.Host;
using Spe.Core.Settings;
using Spe.Core.Utility;

namespace Spe.Integrations.Processors
{
    public abstract class BaseScriptedDataSource
    {
        protected static bool IsScripted(string dataSource)
        {
            const string patternId = @"^script:{[a-zA-Z0-9]{8}-([a-zA-Z0-9]{4}-){3}[a-zA-Z0-9]{12}}$";

            return dataSource != null &&
                   (dataSource.IndexOf("script:", StringComparison.OrdinalIgnoreCase) == 0 &&
                    (dataSource.IndexOf(ApplicationSettings.ScriptLibraryPath, StringComparison.OrdinalIgnoreCase) > -1) ||
                    Regex.IsMatch(dataSource, patternId, RegexOptions.Compiled));
        }

        protected static string GetScriptedQueries(string sources, Item contextItem, ItemList items)
        {
            var unusedLocations = string.Empty;
            foreach (var location in new ListString(sources))
            {
                if (IsScripted(location))
                {
                    items.AddRange(RunEnumeration(location, contextItem));
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

            scriptSource = scriptSource.Replace("script:", "").Trim();
            var database = item?.Database ?? Sitecore.Context.ContentDatabase ?? Sitecore.Context.Database;
            var scriptItem = ID.IsID(scriptSource) ? database.GetItem(ID.Parse(scriptSource)) : database.GetItem(scriptSource);
            if (scriptItem == null || !scriptItem.IsPowerShellScript() 
                || string.IsNullOrWhiteSpace(scriptItem[Templates.Script.Fields.ScriptBody])
                && !RulesUtils.EvaluateRules(scriptItem[Templates.Script.Fields.EnableRule], item))
            {
                return new[] { scriptItem ?? item };
            }

            using (var session = ScriptSessionManager.NewSession(ApplicationNames.Default, false))
            {
                if (item != null)
                {
                    session.SetItemLocationContext(item);
                }
                    
                return session.ExecuteScriptPart(scriptItem, false).Where(i => i is Item).Cast<Item>();
            }
        }
    }
}