using System;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.PowerShellIntegrations
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
    }
}