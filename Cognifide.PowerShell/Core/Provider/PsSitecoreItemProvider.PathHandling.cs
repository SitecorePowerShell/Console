using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.StringExtensions;

namespace Cognifide.PowerShell.Core.Provider
{
    public partial class PsSitecoreItemProvider
    {
        private static readonly char[] delimiters = {'\\', '/', '`'};

        private Item GetItemForPath(string path)
        {
            var colonIndex = path.IndexOf(':');
            var relativePath = path.Substring(colonIndex + 1).Replace('\\', '/');
            var databaseName = colonIndex < 0 ? PSDriveInfo.Name : path.Substring(0, colonIndex);
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
            var result = GetItemForPath(path) != null;
            return result;
        }

        protected override bool ConvertPath(string path, string filter, ref string updatedPath, ref string updatedFilter)
        {
            try
            {
                if (!String.IsNullOrEmpty(filter) || path.IndexOfAny(delimiters) > 0)
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
            var parent = PathUtilities.GetParentFromPath(path);
            var name = PathUtilities.GetLeafFromPath(path);
            //try get literal path
            var literalName = $"/sitecore{parent}/{name}";
            var literalItem = Factory.GetDatabase(PSDriveInfo.Name).GetItem(literalName);
            if (literalItem != null && literalName.Is(literalItem.Paths.Path))
            {
                return new[] {$"{literalItem.Database.Name}:{literalItem.Paths.Path.Substring(9).Replace('/', '\\')}"};
            }
            name = name.Trim(Convert.ToChar("*"));
            if (parent.Contains("-") || parent.Contains(" "))
            {
                var segments = parent.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
                var escapedPath = new StringBuilder(path.Length + segments.Length*2 + 4);
                foreach (var segment in segments)
                {
                    escapedPath.AppendFormat("/#{0}#", segment);
                }
                parent = escapedPath.ToString();
            }
            var items = Factory.GetDatabase(PSDriveInfo.Name).SelectItems(
                $"/sitecore{parent}/*[startswith(@@Name, '{name}')] ");
            var results = items.Select(
                item => $"{item.Database.Name}:{item.Paths.Path.Substring(9).Replace('/', '\\')}").ToArray();
            return results;
        }

        private string NormalizePath(string path)
        {
            string normalizedPath = path;
            if (!path.IsNullOrEmpty())
            {
                normalizedPath = path.Replace('/', '\\');
                if (PathUtilities.HasRelativePathTokens(path))
                {
                    normalizedPath = NormalizeRelativePath(normalizedPath, null);
                }
            }
            return normalizedPath;
        }
    }
}