using System;
using System.Text;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.IO;

namespace Cognifide.PowerShell.Core.Utility
{
    public static class PathUtilities
    {
        public static Item GetItem(string drive, string itemPath)
        {
            var currentDb = Factory.GetDatabase(drive);
            itemPath = itemPath.Replace('\\', '/');
            if (!itemPath.StartsWith("/"))
            {
                itemPath = '/' + itemPath;
            }
            if (!itemPath.StartsWith("/sitecore", StringComparison.OrdinalIgnoreCase))
            {
                itemPath = "/sitecore" + itemPath;
            }
            return currentDb.GetItem(itemPath);
        }

        public static string GetDrive(string path, string currentDb)
        {
            if (String.IsNullOrEmpty(path) || !path.Contains(":")) return currentDb;

            //path with drive
            var drivepath = path.Split(':');
            return drivepath[0];
        }

        public static Item GetItem(string path, string currentDb, string currentPath)
        {
            Item item;
            if (path.Contains(":"))
            {
                //path with drive
                var drivepath = path.Split(':');
                var drive = drivepath[0];
                var itemPath = drivepath[1];
                item = GetItem(drive, itemPath);
            }
            else if (path.StartsWith("/sitecore", StringComparison.OrdinalIgnoreCase))
            {
                item = GetItem(currentDb, path);
            }
            else
            {
                item = GetItem(currentDb, currentPath + '/' + path);
            }
            return item;
        }

        public static string GetProviderPath(this Item item)
        {
            if (item == null)
            {
                return string.Empty;
            }
            var psPath = string.Format("{0}:{1}", item.Database.Name, item.Paths.Path.Substring(9).Replace('/', '\\'));
            return psPath;
        }

        public static string PreparePathForQuery(string path)
        {
            var parts = path.Split('/');
            var sb = new StringBuilder(path.Length + 10);
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part))
                {
                    continue;
                }
                if (part.IndexOf(' ') > -1 && part.IndexOf('#') != 0)
                {
                    sb.AppendFormat("/#{0}#", part);
                }
                else
                {
                    sb.AppendFormat("/{0}", part);
                }
            }
            return sb.ToString();
        }

        public static string GetRelativePath(string absolutePath)
        {
            var siteRoot = FileUtil.MapPath("/");
            var relativePath = absolutePath;
            if (relativePath.StartsWith(siteRoot, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath.Substring(siteRoot.Length - 1).Replace('\\', '/');
            }

            return relativePath;
        }
    }
}