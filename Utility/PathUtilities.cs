using System;
using System.Text;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Utility
{
    public static class PathUtilities
    {
        public static Item GetItem(string drive, string itemPath)
        {
            Database currentDb = Factory.GetDatabase(drive);
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

        public static Item GetItem(string path, string currentDb, string currentPath)
        {
            Item item;
            if (path.Contains(":"))
            {
                //path with drive
                string[] drivepath = path.Split(':');
                string drive = drivepath[0];
                string itemPath = drivepath[1];
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

        public static string GetItemPsPath(Item item)
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
            var sb = new StringBuilder(path.Length+10);
            foreach (var part in parts)
            {
                if(string.IsNullOrEmpty(part))
                {
                    continue;
                }
                if (part.IndexOf(' ') > -1 && part.IndexOf('#') != 0)
                {
                    sb.AppendFormat("/#{0}#", part);
                }
                else
                {
                    sb.AppendFormat("/{0}",part);
                }
            }
            return sb.ToString();
        }
    }
}