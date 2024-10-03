using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Security.Accounts;
using Sitecore.SecurityModel;
using Spe.Core.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Spe.Core.Settings
{
    public static class DelegatedAccessManager
    {
        private static readonly ConcurrentDictionary<string, DelegatedAccessEntry> _accessEntries =
            new ConcurrentDictionary<string, DelegatedAccessEntry>();

        private static readonly List<Item> _delegatedItems = new List<Item>();

        public const string DelegatedItemPath = "/sitecore/system/Modules/PowerShell/Delegated Access";

        private static bool _isInitialized = false;

        public static void Invalidate()
        {
            PowerShellLog.Audit($"Clearing {nameof(DelegatedAccessEntry)} entries.");
            _accessEntries.Clear();
            _delegatedItems.Clear();
            _isInitialized = false;
        }

        private static IEnumerable<Item> GetDelegatedItems()
        {
            if (_isInitialized)
            {
                return _delegatedItems;
            }

            using (new SecurityDisabler())
            {
                var db = Factory.GetDatabase(ApplicationSettings.ScriptLibraryDb);
                var delegatedItems = db.GetItem(DelegatedItemPath)
                    .Axes.GetDescendants()
                    .Where(d => d.TemplateID == Templates.DelegatedAccess.Id);

                _delegatedItems.AddRange(delegatedItems);
            }

            _isInitialized = true;
            return _delegatedItems;
        }

        private static DelegatedAccessEntry GetDelegatedAccessEntry(User currentUser, Item scriptItem)
        {
            var delegatedItems = GetDelegatedItems();

            foreach (var delegatedItem in delegatedItems)
            {
                var entry = GetDelegatedCachedEntry(scriptItem, delegatedItem, currentUser);
                if (entry != null && entry.IsElevated)
                {
                    return entry;
                }
            }

            return null;
        }

        public static User GetDelegatedUser(User currentUser, Item scriptItem)
        {
            var entry = GetDelegatedAccessEntry(currentUser, scriptItem);
            if (entry != null) return entry.ImpersonatedUser ?? entry.CurrentUser;

            return currentUser;
        }

        public static bool IsElevated(User currentUser, Item scriptItem)
        {
            var entry = GetDelegatedAccessEntry(currentUser, scriptItem);
            if (entry != null) return entry.IsElevated;

            return false;
        }

        private static DelegatedAccessEntry GetDeniedElevation(string cacheKey, User currentUser)
        {
            var entry = new DelegatedAccessEntry
            {
                CurrentUser = currentUser
            };

            _accessEntries.TryAdd(cacheKey, entry);

            return entry;
        }

        private static DelegatedAccessEntry GetDelegatedCachedEntry(Item scriptItem, Item delegatedItem, User currentUser)
        {
            var cacheKey = $"{currentUser.Name}-{scriptItem.ID}-{delegatedItem.ID}";
            if(_accessEntries.TryGetValue(cacheKey, out var entry))
            {
                return entry;
            }

            var isEnabled = MainUtil.GetBool(delegatedItem.Fields[Templates.DelegatedAccess.Fields.Enabled].Value, false);
            if (!isEnabled) return GetDeniedElevation(cacheKey, currentUser);

            var impersonatedUserName = delegatedItem.Fields[Templates.DelegatedAccess.Fields.ImpersonatedUser].Value;
            if (string.IsNullOrEmpty(impersonatedUserName)) return GetDeniedElevation(cacheKey, currentUser);

            var impersonatedUser = User.FromName(impersonatedUserName, true);
            if (impersonatedUser == null) return GetDeniedElevation(cacheKey, currentUser);

            var elevatedRoleName = delegatedItem.Fields[Templates.DelegatedAccess.Fields.ElevatedRole].Value;
            if (string.IsNullOrEmpty(elevatedRoleName)) return GetDeniedElevation(cacheKey, currentUser);

            var elevatedRole = Role.FromName(elevatedRoleName);
            if (elevatedRole == null) return GetDeniedElevation(cacheKey, currentUser);

            var selectedItemIds = delegatedItem.Fields[Templates.DelegatedAccess.Fields.ScriptItemId].Value?
                .Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries);

            var itemInList = false;
            foreach (var selectedItemId in selectedItemIds)
            {
                if (ID.Parse(selectedItemId) == scriptItem.ID)
                {
                    itemInList = true;
                    break;
                }
            }

            if (!itemInList) { return GetDeniedElevation(cacheKey, currentUser); }

            if (RolesInRolesManager.IsUserInRole(currentUser, elevatedRole, true))
            {
                entry = new DelegatedAccessEntry
                {
                    DelegatedAccessItemId = delegatedItem.ID,
                    CurrentUser = currentUser,
                    ElevatedRole = elevatedRole,
                    ImpersonatedUser = impersonatedUser,
                    IsElevated = true
                };

                _accessEntries.TryAdd(cacheKey, entry);

                return entry;
            }

            return GetDeniedElevation(cacheKey, currentUser);
        }
    }

    internal class DelegatedAccessEntry
    {
        public ID DelegatedAccessItemId { get; set; }
        public User CurrentUser { get; set; }
        public Role ElevatedRole { get; set; }
        public User ImpersonatedUser { get; set; }
        public bool IsElevated { get; internal set; }
    }
}
