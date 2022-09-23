using System;
using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Events;
using Sitecore.SecurityModel;
using Spe.Core.Settings;

namespace Spe.Core.Modules
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
            if (EventDisabler.IsActive) return;

            Assert.ArgumentNotNull(args, "args");
            var item = Event.ExtractParameter<Item>(args, 0);
            if (IsPowerShellMonitoredItem(item))
            {
                ModuleManager.Invalidate();
            }            
        }

        internal void OnItemDeletedRemote(object sender, EventArgs args)
        {
            if (EventDisabler.IsActive) return;

            Assert.ArgumentNotNull(args, "args");
            if (!(args is ItemDeletedRemoteEventArgs idreArgs))
            {
                return;
            }

            var item = idreArgs.Item.Database.GetItem(idreArgs.ParentId);
            using (new SecurityDisabler())
            {
                if (IsPowerShellMonitoredItem(item))
                {
                    ModuleManager.Invalidate();
                }
            }
        }

        internal void OnItemMoved(object sender, EventArgs args)
        {
            if (EventDisabler.IsActive) return;

            Assert.ArgumentNotNull(args, "args");
            var item = Event.ExtractParameter<Item>(args, 0);
            if (IsPowerShellMonitoredItem(item))
            {
                var itemId = Event.ExtractParameter<ID>(args, 1);
                using (new SecurityDisabler())
                {
                    ModuleManager.Invalidate();
                }
            }
        }

        internal void OnItemSaved(object sender, EventArgs args)
        {
            if (EventDisabler.IsActive) return;

            Assert.ArgumentNotNull(args, "args");
            var item = Event.ExtractParameter<Item>(args, 0);
            if (IsPowerShellMonitoredItem(item))
            {
                ModuleManager.Invalidate();
            }
        }

        internal void OnItemSavedRemote(object sender, EventArgs args)
        {
            if (EventDisabler.IsActive) return;

            Assert.ArgumentNotNull(args, "args");
            if (args is ItemSavedRemoteEventArgs isreErgs)
            {
                ModuleManager.Invalidate();
            }
        }

        internal void OnItemSaving(object sender, EventArgs args)
        {
            if (EventDisabler.IsActive) return;

            Assert.ArgumentNotNull(args, "args");
            var sargs = args as SitecoreEventArgs;
            if (sargs?.Parameters == null) return;

            if (sargs.Parameters[0] is Item item && item.TemplateID == ModuleTemplateId)
            {
                item[Sitecore.FieldIDs.Icon] = item["Enabled"] == "1"
                    ? "Office/32x32/jar_coffee_bean.png"
                    : "Office/32x32/jar.png";
            }
        }
    }
}