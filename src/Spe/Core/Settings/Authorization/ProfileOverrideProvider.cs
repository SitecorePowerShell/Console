using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.SecurityModel;
using Spe.Core.Diagnostics;

namespace Spe.Core.Settings.Authorization
{
    /// <summary>
    /// Reads Restriction Profile Override items from the Sitecore content tree
    /// and merges them with config-based profiles. Overrides are additive only:
    /// blocklist profiles can add more blocked commands, allowlist profiles can
    /// add more allowed commands. Most restrictive wins on conflict.
    ///
    /// Override items live under:
    /// /sitecore/system/Modules/PowerShell/Settings/Restriction Profiles/
    ///
    /// Cached using HttpRuntime.Cache with TTL from Spe.AuthorizationCacheExpirationSecs.
    /// NOTE: May revisit cache invalidation strategy later (event-based vs TTL).
    /// </summary>
    public static class ProfileOverrideProvider
    {
        private const string SettingsPath = "/sitecore/system/Modules/PowerShell/Settings/Remoting/Restriction Profiles";
        private const string CacheKeyPrefix = "Spe.ProfileOverride.";

        /// <summary>
        /// Returns a profile with item-based overrides merged on top of the config profile.
        /// If no override items exist for the profile, returns the original config profile unchanged.
        /// </summary>
        public static RestrictionProfile GetMergedProfile(RestrictionProfile configProfile)
        {
            if (configProfile == null) return null;

            var cacheKey = CacheKeyPrefix + configProfile.Name;
            var cached = HttpRuntime.Cache.Get(cacheKey) as RestrictionProfile;
            if (cached != null) return cached;

            var merged = MergeWithOverrides(configProfile);

            var ttl = WebServiceSettings.AuthorizationCacheExpirationSecs;
            HttpRuntime.Cache.Insert(cacheKey, merged, null,
                DateTime.UtcNow.AddSeconds(ttl),
                System.Web.Caching.Cache.NoSlidingExpiration);

            return merged;
        }

        private static RestrictionProfile MergeWithOverrides(RestrictionProfile configProfile)
        {
            List<Item> overrideItems;
            try
            {
                overrideItems = GetOverrideItems(configProfile.Name);
            }
            catch (Exception ex)
            {
                PowerShellLog.Error("ProfileOverrideProvider: failed to read override items.", ex);
                return configProfile;
            }

            if (overrideItems == null || overrideItems.Count == 0)
            {
                return configProfile;
            }

            // Start with copies of the config profile's restrictions
            var mergedCommands = new HashSet<string>(configProfile.Commands, StringComparer.OrdinalIgnoreCase);
            var mergedPaths = configProfile.ItemPaths != null
                ? new HashSet<string>(configProfile.ItemPaths.Paths, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var auditLevel = configProfile.AuditLevel;

            foreach (var item in overrideItems)
            {
                // Merge additional blocked commands (only for blocklist profiles)
                if (configProfile.CommandMode == CommandRestrictionMode.Blocklist)
                {
                    var blocked = ParseMultiLineField(item, "Additional Blocked Commands");
                    foreach (var cmd in blocked)
                    {
                        mergedCommands.Add(cmd);
                    }
                }

                // Merge additional allowed commands (only for allowlist profiles)
                if (configProfile.CommandMode == CommandRestrictionMode.Allowlist)
                {
                    var allowed = ParseMultiLineField(item, "Additional Allowed Commands");
                    foreach (var cmd in allowed)
                    {
                        mergedCommands.Add(cmd);
                    }
                }

                // Audit level override (last override wins)
                var auditOverride = item.Fields["Audit Level Override"]?.Value?.Trim();
                if (!string.IsNullOrEmpty(auditOverride))
                {
                    if (Enum.TryParse<AuditLevel>(auditOverride, true, out var parsed))
                    {
                        auditLevel = parsed;
                    }
                }

                // Merge additional blocked paths (only for blocklist item path profiles)
                if (configProfile.ItemPaths != null && configProfile.ItemPaths.Mode == CommandRestrictionMode.Blocklist)
                {
                    var blockedPaths = ResolveTreelistPaths(item, "Additional Blocked Paths");
                    foreach (var path in blockedPaths)
                    {
                        mergedPaths.Add(path);
                    }
                }

                // Merge additional allowed paths (only for allowlist item path profiles)
                if (configProfile.ItemPaths != null && configProfile.ItemPaths.Mode == CommandRestrictionMode.Allowlist)
                {
                    var allowedPaths = ResolveTreelistPaths(item, "Additional Allowed Paths");
                    foreach (var path in allowedPaths)
                    {
                        mergedPaths.Add(path);
                    }
                }
            }

            // Only create a new profile if something actually changed
            var configPathCount = configProfile.ItemPaths?.Paths.Count ?? 0;
            var commandsChanged = !mergedCommands.SetEquals(configProfile.Commands);
            var pathsChanged = mergedPaths.Count != configPathCount ||
                               (configProfile.ItemPaths != null && !mergedPaths.SetEquals(configProfile.ItemPaths.Paths));
            var auditChanged = auditLevel != configProfile.AuditLevel;

            if (!commandsChanged && !pathsChanged && !auditChanged)
            {
                return configProfile;
            }

            PowerShellLog.Info($"ProfileOverrideProvider: merged {overrideItems.Count} override(s) into profile '{configProfile.Name}' " +
                             $"(commands: {configProfile.Commands.Count} -> {mergedCommands.Count}, " +
                             $"paths: {configPathCount} -> {mergedPaths.Count}, " +
                             $"auditLevel: {configProfile.AuditLevel} -> {auditLevel})");

            var mergedItemPaths = configProfile.ItemPaths != null
                ? new ItemPathRestrictions(configProfile.ItemPaths.Mode, mergedPaths)
                : null;

            return new RestrictionProfile(
                configProfile.Name,
                configProfile.LanguageMode,
                configProfile.CommandMode,
                mergedCommands,
                configProfile.Modules,
                auditLevel,
                configProfile.Enforcement,
                mergedItemPaths);
        }

        private static List<Item> GetOverrideItems(string profileName)
        {
            var db = Factory.GetDatabase(ApplicationSettings.ScriptLibraryDb);
            if (db == null)
            {
                PowerShellLog.Warn("ProfileOverrideProvider: database is null.");
                return null;
            }

            Item settingsFolder;
            using (new SecurityDisabler())
            {
                settingsFolder = db.GetItem(SettingsPath);
            }

            if (settingsFolder == null)
            {
                PowerShellLog.Debug($"ProfileOverrideProvider: settings folder not found at '{SettingsPath}'.");
                return null;
            }

            var results = new List<Item>();
            using (new SecurityDisabler())
            {
                CollectOverridesRecursive(settingsFolder, profileName, results);
            }

            PowerShellLog.Debug($"ProfileOverrideProvider: found {results.Count} override(s) for profile '{profileName}'.");
            return results;
        }

        private static void CollectOverridesRecursive(Item folder, string profileName, List<Item> results)
        {
            foreach (Item child in folder.GetChildren())
            {
                if (child.TemplateID == Templates.RestrictionProfile.Id)
                {
                    var enabledField = child.Fields[Templates.RestrictionProfile.Fields.Enabled];
                    if (enabledField != null && enabledField.Value != "1")
                    {
                        PowerShellLog.Debug($"ProfileOverrideProvider: skipping disabled override '{child.Name}'.");
                        continue;
                    }

                    var baseProfile = child.Fields["Base Profile"]?.Value?.Trim();
                    if (string.IsNullOrEmpty(baseProfile))
                    {
                        PowerShellLog.Warn($"ProfileOverrideProvider: override item '{child.Name}' ({child.ID}) has an empty Base Profile field and will be ignored.");
                        continue;
                    }

                    if (!RestrictionProfileManager.ProfileExists(baseProfile))
                    {
                        PowerShellLog.Warn($"ProfileOverrideProvider: override item '{child.Name}' ({child.ID}) references unknown profile '{baseProfile}'. " +
                                           "Check that the Base Profile value matches a profile defined in Spe.config.");
                        continue;
                    }

                    if (string.Equals(baseProfile, profileName, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(child);
                    }
                }
                else if (child.HasChildren)
                {
                    CollectOverridesRecursive(child, profileName, results);
                }
            }
        }

        /// <summary>
        /// Resolves a Treelist field (pipe-delimited GUIDs) to Sitecore item paths.
        /// Skips GUIDs that cannot be resolved (deleted items).
        /// </summary>
        private static IEnumerable<string> ResolveTreelistPaths(Item item, string fieldName)
        {
            var value = item.Fields[fieldName]?.Value;
            if (string.IsNullOrEmpty(value)) return Enumerable.Empty<string>();

            var db = item.Database;
            var paths = new List<string>();

            foreach (var guidStr in value.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!ID.TryParse(guidStr.Trim(), out var id)) continue;

                var target = db.GetItem(id);
                if (target != null)
                {
                    paths.Add(target.Paths.Path);
                }
                else
                {
                    PowerShellLog.Warn($"ProfileOverrideProvider: Treelist field '{fieldName}' on '{item.Name}' references item {guidStr} which could not be resolved.");
                }
            }

            return paths;
        }

        private static IEnumerable<string> ParseMultiLineField(Item item, string fieldName)
        {
            var value = item.Fields[fieldName]?.Value;
            if (string.IsNullOrEmpty(value)) return Enumerable.Empty<string>();

            return value.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(line => line.Trim())
                        .Where(line => !string.IsNullOrEmpty(line));
        }
    }
}
