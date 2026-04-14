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
        private static readonly ConcurrentDictionary<string, CachedAccessEntry> _accessEntries =
            new ConcurrentDictionary<string, CachedAccessEntry>();

        private static readonly object _initLock = new object();
        private static List<DelegatedAccessConfig> _delegatedConfigs;
        private static DateTime _lastCleanupUtc = DateTime.UtcNow;

        private const int DefaultCacheTtlSeconds = 300;
        private const int CleanupIntervalSeconds = 60;

        public static string DelegatedItemPath
        {
            get
            {
                var db = Factory.GetDatabase(ApplicationSettings.ScriptLibraryDb);
                var item = db?.GetItem(Templates.Items.DelegatedAccess);
                return item?.Paths.Path;
            }
        }

        internal static int CacheTtlSeconds
        {
            get
            {
                try
                {
                    return Sitecore.Configuration.Settings.GetIntSetting("Spe.DelegatedAccessCacheTtlSeconds", DefaultCacheTtlSeconds);
                }
                catch
                {
                    return DefaultCacheTtlSeconds;
                }
            }
        }

        public static void Invalidate()
        {
            PowerShellLog.Debug("[DelegatedAccess] action=cacheInvalidated");
            _accessEntries.Clear();
            lock (_initLock)
            {
                _delegatedConfigs = null;
            }
        }

        private static IEnumerable<DelegatedAccessConfig> GetDelegatedConfigs()
        {
            var configs = _delegatedConfigs;
            if (configs != null)
            {
                return configs;
            }

            lock (_initLock)
            {
                if (_delegatedConfigs != null)
                {
                    return _delegatedConfigs;
                }

                using (new SecurityDisabler())
                {
                    var db = Factory.GetDatabase(ApplicationSettings.ScriptLibraryDb);
                    var parent = db.GetItem(Templates.Items.DelegatedAccess);
                    _delegatedConfigs = parent?.Axes.GetDescendants()
                        .Where(d => d.TemplateID == Templates.DelegatedAccess.Id)
                        .Select(DelegatedAccessConfig.FromItem)
                        .ToList() ?? new List<DelegatedAccessConfig>();
                }

                return _delegatedConfigs;
            }
        }

        private static DelegatedAccessEntry GetDelegatedAccessEntry(User currentUser, Item scriptItem)
        {
            var configs = GetDelegatedConfigs();

            foreach (var config in configs)
            {
                var entry = GetDelegatedCachedEntry(scriptItem, config, currentUser);
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

            var cached = new CachedAccessEntry
            {
                Entry = entry,
                ExpiresUtc = DateTime.UtcNow.AddSeconds(CacheTtlSeconds)
            };

            _accessEntries.TryAdd(cacheKey, cached);

            return entry;
        }

        private static void CleanupExpiredEntries()
        {
            var now = DateTime.UtcNow;
            if ((now - _lastCleanupUtc).TotalSeconds < CleanupIntervalSeconds) return;

            _lastCleanupUtc = now;
            var expiredKeys = _accessEntries
                .Where(kvp => kvp.Value.ExpiresUtc <= now)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _accessEntries.TryRemove(key, out _);
            }

            if (expiredKeys.Count > 0)
            {
                PowerShellLog.Debug($"[DelegatedAccess] action=cacheCleanup removed={expiredKeys.Count} remaining={_accessEntries.Count}");
            }
        }

        private static DelegatedAccessEntry GetDelegatedCachedEntry(Item scriptItem, DelegatedAccessConfig config, User currentUser)
        {
            var cacheKey = $"{currentUser.Name}-{scriptItem.ID}-{config.Id}";

            if (_accessEntries.TryGetValue(cacheKey, out var cached))
            {
                if (cached.ExpiresUtc > DateTime.UtcNow)
                {
                    return cached.Entry;
                }

                _accessEntries.TryRemove(cacheKey, out _);
            }

            CleanupExpiredEntries();

            if (!config.Enabled) return GetDeniedElevation(cacheKey, currentUser);

            if (string.IsNullOrEmpty(config.ImpersonatedUserName)) return GetDeniedElevation(cacheKey, currentUser);

            var impersonatedUser = User.FromName(config.ImpersonatedUserName, true);
            if (impersonatedUser == null) return GetDeniedElevation(cacheKey, currentUser);

            if (string.IsNullOrEmpty(config.ElevatedRoleName)) return GetDeniedElevation(cacheKey, currentUser);

            var elevatedRole = Role.FromName(config.ElevatedRoleName);
            if (elevatedRole == null) return GetDeniedElevation(cacheKey, currentUser);

            if (!config.ScriptItemIds.Contains(scriptItem.ID)) return GetDeniedElevation(cacheKey, currentUser);

            if (RolesInRolesManager.IsUserInRole(currentUser, elevatedRole, true))
            {
                var entry = new DelegatedAccessEntry
                {
                    DelegatedAccessItemId = config.Id,
                    CurrentUser = currentUser,
                    ElevatedRole = elevatedRole,
                    ImpersonatedUser = impersonatedUser,
                    IsElevated = true
                };

                var cachedEntry = new CachedAccessEntry
                {
                    Entry = entry,
                    ExpiresUtc = DateTime.UtcNow.AddSeconds(CacheTtlSeconds)
                };

                _accessEntries.TryAdd(cacheKey, cachedEntry);

                return entry;
            }

            return GetDeniedElevation(cacheKey, currentUser);
        }
    }

    internal class DelegatedAccessConfig
    {
        public ID Id { get; set; }
        public bool Enabled { get; set; }
        public string ElevatedRoleName { get; set; }
        public string ImpersonatedUserName { get; set; }
        public HashSet<ID> ScriptItemIds { get; set; }

        public static DelegatedAccessConfig FromItem(Item item)
        {
            var scriptItemIdValue = item.Fields[Templates.DelegatedAccess.Fields.ScriptItemId].Value;
            var scriptItemIds = new HashSet<ID>();
            if (!string.IsNullOrEmpty(scriptItemIdValue))
            {
                foreach (var id in scriptItemIdValue.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (ID.TryParse(id, out var parsed))
                    {
                        scriptItemIds.Add(parsed);
                    }
                }
            }

            return new DelegatedAccessConfig
            {
                Id = item.ID,
                Enabled = MainUtil.GetBool(item.Fields[Templates.DelegatedAccess.Fields.Enabled].Value, false),
                ElevatedRoleName = item.Fields[Templates.DelegatedAccess.Fields.ElevatedRole].Value,
                ImpersonatedUserName = item.Fields[Templates.DelegatedAccess.Fields.ImpersonatedUser].Value,
                ScriptItemIds = scriptItemIds
            };
        }
    }

    internal class CachedAccessEntry
    {
        public DelegatedAccessEntry Entry { get; set; }
        public DateTime ExpiresUtc { get; set; }
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
