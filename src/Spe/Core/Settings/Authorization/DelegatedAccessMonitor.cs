using Sitecore;
using Sitecore.Data.Events;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Events;
using Sitecore.Security.Accounts;
using Spe.Core.Diagnostics;
using System;
using System.Collections.Generic;
using System.Web.Security;

namespace Spe.Core.Settings.Authorization
{
    public class DelegatedAccessMonitor
    {
        protected bool IsPowerShellMonitoredItem(Item item)
        {
            if (item == null) return false;

            return item.ID == ItemIDs.DelegatedAccess ||
                   (item.Parent != null && item.Parent.ID == ItemIDs.DelegatedAccess);
        }

        internal void OnItemSaved(object sender, EventArgs args)
        {
            if (EventDisabler.IsActive) return;

            Assert.ArgumentNotNull(args, "args");
            var item = Event.ExtractParameter<Item>(args, 0);
            if (IsPowerShellMonitoredItem(item))
            {
                if (item.TemplateID == Templates.DelegatedAccess.Id)
                {
                    var enabled = MainUtil.GetBool(item.Fields[Templates.DelegatedAccess.Fields.Enabled].Value, false);
                    PowerShellLog.Audit($"[DelegatedAccess] action=configChanged item=\"{item.Name} {item.ID}\" enabled={enabled}");
                }

                DelegatedAccessManager.Invalidate();
            }
        }

        internal void OnItemDeleting(object sender, EventArgs args)
        {
            if (EventDisabler.IsActive) return;

            Assert.ArgumentNotNull(args, "args");
            var item = Event.ExtractParameter<Item>(args, 0);
            if (IsPowerShellMonitoredItem(item))
            {
                if (item.TemplateID == Templates.DelegatedAccess.Id)
                {
                    PowerShellLog.Audit($"[DelegatedAccess] action=configDeleted item=\"{item.Name} {item.ID}\"");
                }

                DelegatedAccessManager.Invalidate();
            }
        }

        internal void OnRoleCreated(object sender, EventArgs args) 
        {
            if (EventDisabler.IsActive) return;

            Assert.ArgumentNotNull(args, "args");
            var roleName = Event.ExtractParameter<string>(args, 0);
            if (string.IsNullOrEmpty(roleName)) return;

            DelegatedAccessManager.Invalidate();
        }

        internal void OnRoleRemoved(object sender, EventArgs args)
        {
            if (EventDisabler.IsActive) return;

            Assert.ArgumentNotNull(args, "args");
            var roleName = Event.ExtractParameter<string>(args, 0);
            if (string.IsNullOrEmpty(roleName)) return;

            DelegatedAccessManager.Invalidate();
        }

        internal void OnRolesInRolesAltered(object sender, EventArgs args)
        {
            if (EventDisabler.IsActive) return;

            Assert.ArgumentNotNull(args, "args");
            var roles = Event.ExtractParameter<IEnumerable<Role>>(args, 0);
            if (roles == null) return;

            DelegatedAccessManager.Invalidate();
        }

        internal void OnRolesInRolesRemoved(object sender, EventArgs args)
        {
            if (EventDisabler.IsActive) return;

            Assert.ArgumentNotNull(args, "args");
            var roleName = Event.ExtractParameter<string>(args, 0);
            if (string.IsNullOrEmpty(roleName)) return;

            DelegatedAccessManager.Invalidate();
        }

        internal void OnUserCreated(object sender, EventArgs args)
        {
            if (EventDisabler.IsActive) return;

            Assert.ArgumentNotNull(args, "args");
            var user = Event.ExtractParameter<MembershipUser>(args, 0);
            if (user == null || string.IsNullOrEmpty(user.UserName)) return;

            DelegatedAccessManager.Invalidate();
        }

        internal void OnUserRemoved(object sender, EventArgs args)
        {
            if (EventDisabler.IsActive) return;

            Assert.ArgumentNotNull(args, "args");
            var username = Event.ExtractParameter<string>(args, 0);
            if (string.IsNullOrEmpty(username)) return;

            DelegatedAccessManager.Invalidate();
        }

        internal void OnUserUpdated(object sender, EventArgs args)
        {
            if (EventDisabler.IsActive) return;

            Assert.ArgumentNotNull(args, "args");
            var user = Event.ExtractParameter<MembershipUser>(args, 0);
            if (user == null || string.IsNullOrEmpty(user.UserName)) return;

            DelegatedAccessManager.Invalidate();
        }

        internal void OnRoleReferenceUpdated(object sender, EventArgs args)
        {
            if (EventDisabler.IsActive) return;

            Assert.ArgumentNotNull(args, "args");
            var data = Event.ExtractParameter<object>(args, 0);
            if (data == null) return;
            var username = ((string[])data)[0];
            if (string.IsNullOrEmpty(username)) return;

            DelegatedAccessManager.Invalidate();
        }
    }
}
