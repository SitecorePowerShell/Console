using System;
using Cognifide.PowerShell.Core.Settings;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Events;
using Sitecore.SecurityModel;

namespace Cognifide.PowerShell.Core.Modules
{
    public class ModuleMonitor
    {
        private static readonly ID ModuleTemplateId = new ID("{6D82FCD8-C379-443C-97A9-C6423C71E7D5}");

        protected bool IsPowerShellMonitoredItem(Item item)
        {
            return (item != null) &&
                   item.Paths.Path.StartsWith(ApplicationSettings.ScriptLibraryPath,
                       StringComparison.InvariantCultureIgnoreCase);
        }

        internal void OnItemDeleted(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            var item = Event.ExtractParameter(args, 0) as Item;
            if (item != null && IsPowerShellMonitoredItem(item))
            {
                ModuleManager.Invalidate(item);
            }
        }

        internal void OnItemDeletedRemote(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            var idreArgs = args as ItemDeletedRemoteEventArgs;
            if (idreArgs != null)
            {
                var item = idreArgs.Item.Database.GetItem(idreArgs.ParentId);
                using (new SecurityDisabler())
                {
                    if (item != null && IsPowerShellMonitoredItem(item))
                    {
                        ModuleManager.Invalidate(item);
                    }
                }
            }
        }

        internal void OnItemMoved(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            var item = Event.ExtractParameter<Item>(args, 0);
            var itemId = Event.ExtractParameter<ID>(args, 1);
            using (new SecurityDisabler())
            {
                ModuleManager.Invalidate(item.Parent);
                ModuleManager.Invalidate(item.Database.GetItem(itemId));
            }
        }

        internal void OnItemSaved(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            var item = Event.ExtractParameter(args, 0) as Item;
            if (IsPowerShellMonitoredItem(item))
            {
                ModuleManager.Invalidate(item);
            }
        }

        internal void OnItemSavedRemote(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            var isreErgs = args as ItemSavedRemoteEventArgs;
            if (isreErgs != null)
            {
                ModuleManager.Invalidate(isreErgs.Item);
            }
        }

        internal void OnItemSaving(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            var sargs = args as SitecoreEventArgs;
            if (sargs != null && sargs.Parameters != null)
            {
                var item = sargs.Parameters[0] as Item;
                if (item.TemplateID == ModuleTemplateId)
                {
                    item[FieldIDs.Icon] = item["Enabled"] == "1"
                        ? "Software/32x32/jar_bean.png"
                        : "Software/32x32/jar_delete.png";
                }
            }
        }
    }
}