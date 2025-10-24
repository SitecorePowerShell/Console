using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.StringExtensions;
using Spe.Core.Extensions;
using Spe.Core.Utility;

namespace Spe.Core.Provider
{
    public partial class PsSitecoreItemProvider
    {
        private static readonly char[] Delimiters = {'\\', '/', '`'};

        private Item GetItemForPath(string path)
        {
            var colonIndex = path.IndexOf(':');
            var relativePath = path.Substring(colonIndex + 1).Replace('\\', '/');
            var databaseName = colonIndex < 0 ? PSDriveInfo?.Name : path.Substring(0, colonIndex);
            var currentItem = PathUtilities.GetItem(databaseName, relativePath);
            return currentItem;
        }

        private Item GetItemById(string partialPath, string id)
        {
            var colonIndex = partialPath.IndexOf(':');
            var databaseName = colonIndex < 0 ? PSDriveInfo.Name : partialPath.Substring(0, colonIndex);
            var db = Factory.GetDatabase(databaseName);
            return db?.GetItem(Sitecore.Data.ID.Parse(id));
        }

        protected override bool IsValidPath(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                    return false;

                LogInfo("Executing IsValidPath(string path='{0}')", path);
                return GetItemForPath(path) != null;
            }
            catch (Exception ex)
            {
                LogError(ex, "Error while executing IsValidPath(string path='{0}')", path);
                throw;
            }
        }

        protected override bool ItemExists(string path)
        {
            try
            {
                var result = GetItemForPath(path) != null;
                LogInfo("Executing ItemExists(string path='{0}') returns '{1}'", path, result);
                return result;
            }
            catch (Exception ex)
            {
                LogError(ex, "Error while executing ItemExists(string path='{0}')", path);
                throw;
            }
        }

        protected override bool IsItemContainer(string path)
        {
            //var result = GetItemForPath(path) != null;
            //return result;
            return true;
        }

        protected override bool ConvertPath(string path, string filter, ref string updatedPath, ref string updatedFilter)
        {
            try
            {
                if (!String.IsNullOrEmpty(filter) || path.IndexOfAny(Delimiters) > 0)
                {
                    return false;
                }
                updatedPath = path;
                updatedFilter = Regex.Replace(path, "\\[.*?\\]", "?");
                LogInfo(
                    "Executing ConvertPath(string path='{0}', string filter='{1}', ref string updatedPath='{2}', ref string updatedFilter='{3}')",
                    path, filter, updatedPath, updatedFilter);
                return true;
            }
            catch (Exception ex)
            {
                LogError(ex,
                    "Error Executing ConvertPath(string path='{0}', string filter='{1}', ref string updatedPath='{2}', ref string updatedFilter='{3}')",
                    path, filter, updatedPath, updatedFilter);
                throw;
            }
        }

        protected override string[] ExpandPath(string path)
        {
            path = path.Substring(path.IndexOf(':') + 1).Replace('\\', '/');
            var pathParent = PathUtilities.GetParentFromPath(path);
            var dbName = PSDriveInfo.Name;
            var results = ExpandSitecorePath(path)
                .Select(p => $"{dbName}:{p.Replace('/', '\\')}")
                .ToArray();
            return results;
        }

        protected List<string> ExpandSitecorePath(string path)
        {
            var pathParent = PathUtilities.GetParentFromPath(path);
            var parents = new List<string> { pathParent };
            if (WildcardPattern.ContainsWildcardCharacters(pathParent))
            {
                parents = ExpandSitecorePath(pathParent);
            }

            var results = new List<string>();
            foreach (var parent in parents)
            {
                var name = PathUtilities.GetLeafFromPath(path);
                var dbName = PSDriveInfo.Name;
                //try get literal path
                var literalName = $"/sitecore{parent}/{name}";
                if (parent.StartsWith("/sitecore", StringComparison.OrdinalIgnoreCase))
                {
                    literalName = $"{parent}/{name}";
                }

                var literalItem = Factory.GetDatabase(dbName).GetItem(literalName);
                if (literalItem != null && literalName.Is(literalItem.Paths.Path))
                {
                    results.Add(literalName);
                    continue;
                }

                var parentItem = Factory.GetDatabase(dbName).GetItem($"/sitecore{parent}");
                var items = WildcardUtils.WildcardFilter($"{name}", parentItem.Children, item => item.Name);
                var paths = items.Select(item => $"{parent}/{item.Name}");
                results.AddRange(paths);
            }
            return results;
        }

        private string NormalizePath(string path)
        {
            var normalizedPath = path;
            if (path.IsNullOrEmpty()) return normalizedPath;

            normalizedPath = path.Replace('/', '\\');
            if (PathUtilities.HasRelativePathTokens(path))
            {
                normalizedPath = NormalizeRelativePath(normalizedPath, null);
            }
            return normalizedPath;
        }
    }
}