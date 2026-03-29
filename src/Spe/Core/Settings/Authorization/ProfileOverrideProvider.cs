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

            // Start with a copy of the config profile's commands
            var mergedCommands = new HashSet<string>(configProfile.Commands, StringComparer.OrdinalIgnoreCase);
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
            }

            // Only create a new profile if something actually changed
            if (mergedCommands.SetEquals(configProfile.Commands) && auditLevel == configProfile.AuditLevel)
            {
                return configProfile;
            }

            PowerShellLog.Info($"ProfileOverrideProvider: merged {overrideItems.Count} override(s) into profile '{configProfile.Name}' " +
                             $"(commands: {configProfile.Commands.Count} -> {mergedCommands.Count}, auditLevel: {configProfile.AuditLevel} -> {auditLevel})");

            return new RestrictionProfile(
                configProfile.Name,
                configProfile.LanguageMode,
                configProfile.CommandMode,
                mergedCommands,
                configProfile.Modules,
                configProfile.StandardFields,
                auditLevel,
                configProfile.Enforcement);
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
                foreach (Item child in settingsFolder.GetChildren())
                {
                    PowerShellLog.Debug($"ProfileOverrideProvider: examining child '{child.Name}' (Template={child.TemplateID})");

                    if (child.TemplateID != Templates.RestrictionProfileOverride.Id) continue;

                    var baseProfile = child.Fields["Base Profile"]?.Value?.Trim();
                    PowerShellLog.Debug($"ProfileOverrideProvider: override item '{child.Name}' has BaseProfile='{baseProfile}'");

                    if (string.Equals(baseProfile, profileName, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(child);
                    }
                }
            }

            PowerShellLog.Debug($"ProfileOverrideProvider: found {results.Count} override(s) for profile '{profileName}'.");
            return results;
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
