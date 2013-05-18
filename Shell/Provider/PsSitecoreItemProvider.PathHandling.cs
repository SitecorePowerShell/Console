using System;
using System.Text.RegularExpressions;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Shell.Provider
{
    public partial class PsSitecoreItemProvider
    {
        private static readonly char[] delimiters = new[] {'\\', '/', '`'};

        private Item GetItemForPath(string path)
        {
            string relativePath = path.Substring(path.IndexOf(':') + 1).Replace('\\', '/');
            string databaseName = path.IndexOf(':') < 0 ? PSDriveInfo.Name : path.Substring(0, path.IndexOf(':'));
            Item currentItem = PathUtilities.GetItem(databaseName, relativePath);
            return currentItem;
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
                bool result = GetItemForPath(path) != null;
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
            return true;
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

        private static string GetParentFromPath(string path)
        {
            path = path.Replace('\\', '/').TrimEnd('/');
            int lastLeafIndex = path.LastIndexOf('/');
            return path.Substring(0, lastLeafIndex);
        }

        private static string GetLeafFromPath(string path)
        {
            path = path.Replace('\\', '/').TrimEnd('/');
            int lastLeafIndex = path.LastIndexOf('/');
            return path.Substring(lastLeafIndex + 1);
        }
    }
}