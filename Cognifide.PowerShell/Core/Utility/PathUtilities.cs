using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        public static string GetSitecorePath(string path)
        {
            if (!path.Contains(":"))
            {
                return EnsureItemPath(path);
            }

            //path with drive
            var drivepath = path.Split(':');
            return EnsureItemPath(drivepath[1]);
        }

        private static string EnsureItemPath(string path)
        {
            path = path.Replace('\\', '/');

            if (String.IsNullOrEmpty(path) || path == "/")
            {
                return "/sitecore";
            }

            if (!path.StartsWith("/"))
            {
                path = "/" + path;
            }
            if (!path.StartsWith("/sitecore", StringComparison.OrdinalIgnoreCase))
            {
                path = "/sitecore" + path;
            }
            return path;
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
                return String.Empty;
            }
            var psPath = String.Format("{0}:{1}", item.Database.Name, item.Paths.Path.Substring(9).Replace('/', '\\'));
            return psPath;
        }

        public static string GetProviderPaths(this IEnumerable<Item> items)
        {
            return items.Select(item => item.GetProviderPath()).Aggregate((seed, curr) => seed + ", " + curr);
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
                if ((part.IndexOf(' ') > -1 || part.IndexOf('-') > -1)  && part.IndexOf('#') != 0)
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

        public static string GetParentFromPath(string path)
        {
            path = path.Replace('\\', '/').TrimEnd('/');
            var lastLeafIndex = path.LastIndexOf('/');
            return path.Substring(0, lastLeafIndex);
        }

        public static string GetLeafFromPath(string path)
        {
            path = path.Replace('\\', '/').TrimEnd('/');
            var lastLeafIndex = path.LastIndexOf('/');
            return path.Substring(lastLeafIndex + 1);
        }

        public static bool HasRelativePathTokens(string path)
        {
            if ((((path.IndexOf(@"\", StringComparison.OrdinalIgnoreCase) != 0) && !path.Contains(@"\.\")) &&
                 (!path.Contains(@"\..\") && !path.EndsWith(@"\..", StringComparison.OrdinalIgnoreCase))) &&
                ((!path.EndsWith(@"\.", StringComparison.OrdinalIgnoreCase) &&
                  !path.StartsWith(@"..\", StringComparison.OrdinalIgnoreCase)) &&
                 !path.StartsWith(@".\", StringComparison.OrdinalIgnoreCase)))
            {
                return path.StartsWith("~", StringComparison.OrdinalIgnoreCase);
            }
            return true;
        }
    }
}