using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Configuration;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Core.Provider
{
    public partial class PsSitecoreItemProvider
    {
        private static readonly char[] delimiters = {'\\', '/', '`'};

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
            bool result = GetItemForPath(path) != null;
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
            string parent = GetParentFromPath(path);
            string name = GetLeafFromPath(path).Trim('*');
            if (parent.Contains("-") || parent.Contains(" "))
            {
                string[] segments = parent.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
                var escapedPath = new StringBuilder(path.Length + segments.Length*2 + 4);
                for (int i = 0; i < segments.Length; i++)
                {
                    escapedPath.AppendFormat("/#{0}#", segments[i]);
                }
                parent = escapedPath.ToString();
            }
            Item[] items = Factory.GetDatabase(PSDriveInfo.Name).SelectItems(
                string.Format("/sitecore{0}/*[startswith(@@Name, '{1}')] ",
                    parent, name));
            string[] results = items.Select(
                item => string.Format("{0}:{1}",
                    item.Database.Name, item.Paths.Path.Substring(9).Replace('/', '\\'))).ToArray();
            return results;
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