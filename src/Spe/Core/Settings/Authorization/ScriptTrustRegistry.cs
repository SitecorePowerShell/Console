using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.SecurityModel;
using Spe.Core.Diagnostics;

namespace Spe.Core.Settings.Authorization
{
    /// <summary>
    /// Registry of trusted scripts loaded from content tree items under
    /// /sitecore/system/Modules/PowerShell/Settings/Remoting/Trusted Scripts/.
    /// Provides O(1) lookup by Sitecore item GUID with content hash verification
    /// and profile-bound trust.
    /// </summary>
    public static class ScriptTrustRegistry
    {
        private static readonly Dictionary<ID, TrustedScriptEntry> _entriesById =
            new Dictionary<ID, TrustedScriptEntry>();

        private static readonly Dictionary<string, TrustedScriptEntry> _entriesByName =
            new Dictionary<string, TrustedScriptEntry>(StringComparer.OrdinalIgnoreCase);

        private static bool _initialized;
        private static readonly object _lock = new object();

        /// <summary>
        /// Looks up a trusted script entry by its Sitecore item ID.
        /// Returns null if the item is not in the trust registry.
        /// </summary>
        public static TrustedScriptEntry GetByItemId(ID itemId)
        {
            EnsureInitialized();
            _entriesById.TryGetValue(itemId, out var entry);
            return entry;
        }

        /// <summary>
        /// Looks up a trusted script entry by its name.
        /// Used for resolution preference in constrained sessions.
        /// Returns null if no trusted script with that name exists.
        /// </summary>
        public static TrustedScriptEntry GetByName(string name)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(name)) return null;
            _entriesByName.TryGetValue(name, out var entry);
            return entry;
        }

        /// <summary>
        /// Evaluates trust for a script item. Returns the trust level and whether
        /// the script's content hash matches.
        /// </summary>
        public static ScriptTrustResult EvaluateTrust(ID itemId, string scriptBody, string activeProfileName = null)
        {
            EnsureInitialized();

            if (!_entriesById.TryGetValue(itemId, out var entry))
            {
                return new ScriptTrustResult(ScriptTrustLevel.Untrusted, true, null);
            }

            // Check profile-bound trust
            if (!entry.IsAllowedForProfile(activeProfileName))
            {
                PowerShellLog.Info(
                    $"[Trust] action=profileMismatch entry={entry.Name} itemId={itemId} profile={activeProfileName}");
                return new ScriptTrustResult(ScriptTrustLevel.Untrusted, true, entry);
            }

            // Verify content hash if configured
            var hashValid = true;
            if (!string.IsNullOrEmpty(entry.ContentHash) && !string.IsNullOrEmpty(scriptBody))
            {
                var actualHash = ComputeHash(scriptBody);
                hashValid = string.Equals(entry.ContentHash, actualHash, StringComparison.OrdinalIgnoreCase);

                if (!hashValid)
                {
                    PowerShellLog.Warn(
                        $"[Trust] action=hashMismatch entry={entry.Name} itemId={itemId} expected={entry.ContentHash} actual={actualHash} onMismatch={entry.OnHashMismatch}");
                }
            }

            return new ScriptTrustResult(entry.Trust, hashValid, entry);
        }

        /// <summary>
        /// Computes SHA256 hash of a script body, returned as "sha256:hex" format.
        /// </summary>
        public static string ComputeHash(string scriptBody)
        {
            if (string.IsNullOrEmpty(scriptBody)) return null;

            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(scriptBody);
                var hashBytes = sha256.ComputeHash(bytes);
                var sb = new StringBuilder("sha256:", 71);
                foreach (var b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private const string TrustedScriptsSettingsPath =
            "/sitecore/system/Modules/PowerShell/Settings/Remoting/Trusted Scripts";

        /// <summary>
        /// Invalidates the trust registry so it will be reloaded on next access.
        /// Called when trust-related items are saved or deleted.
        /// </summary>
        public static void Invalidate()
        {
            lock (_lock)
            {
                _entriesById.Clear();
                _entriesByName.Clear();
                _initialized = false;
            }
        }

        private static void EnsureInitialized()
        {
            if (_initialized) return;
            lock (_lock)
            {
                if (_initialized) return;
                LoadFromItems();
                _initialized = true;
            }
        }

        private static void LoadFromItems()
        {
            try
            {
                var db = Factory.GetDatabase(ApplicationSettings.ScriptLibraryDb);
                if (db == null) return;

                Item settingsFolder;
                using (new SecurityDisabler())
                {
                    settingsFolder = db.GetItem(TrustedScriptsSettingsPath);
                }

                if (settingsFolder == null)
                {
                    PowerShellLog.Debug("[Trust] action=folderNotFound");
                    return;
                }

                using (new SecurityDisabler())
                {
                    CollectTrustEntriesRecursive(settingsFolder);
                }
            }
            catch (Exception ex)
            {
                PowerShellLog.Error("[Trust] action=loadFailed", ex);
            }
        }

        /// <summary>
        /// Recursively loads Trusted Script items from a folder and its subfolders.
        /// </summary>
        private static void CollectTrustEntriesRecursive(Item folder)
        {
            foreach (Item child in folder.GetChildren())
            {
                if (child.TemplateID == Templates.TrustedScript.Id)
                {
                    try
                    {
                        var entries = ParseItemEntries(child);
                        foreach (var entry in entries)
                        {
                            _entriesById[entry.ItemId] = entry;
                            _entriesByName[entry.Name] = entry;
                            PowerShellLog.Info(
                                $"[Trust] action=entryLoaded entry={entry.Name} itemId={entry.ItemId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        PowerShellLog.Error($"[Trust] action=entryLoadFailed entry={child.Name}", ex);
                    }
                }
                else if (child.HasChildren)
                {
                    // Recurse into subfolders (e.g. Core Functions, Web API)
                    CollectTrustEntriesRecursive(child);
                }
            }
        }

        /// <summary>
        /// Parses a Trusted Script item. The Script field is a Treelist (pipe-delimited IDs)
        /// so one trust item can reference multiple script items. Returns entries for each.
        /// </summary>
        private static List<TrustedScriptEntry> ParseItemEntries(Item trustItem)
        {
            var results = new List<TrustedScriptEntry>();

            // Check if enabled
            var enabledField = trustItem.Fields[Templates.TrustedScript.Fields.Enabled];
            if (enabledField != null && enabledField.Value != "1")
            {
                PowerShellLog.Debug($"[Trust] action=entryDisabled entry={trustItem.Name}");
                return results;
            }

            // Script references (Treelist - pipe-delimited item IDs)
            var scriptFieldValue = trustItem.Fields[Templates.TrustedScript.Fields.Script]?.Value;
            if (string.IsNullOrEmpty(scriptFieldValue))
            {
                PowerShellLog.Warn($"[Trust] action=noScriptReferences entry={trustItem.Name}");
                return results;
            }

            // Allowed profiles (shared across all referenced scripts)
            var allowedProfiles = ParseAllowedProfiles(
                trustItem.Fields[Templates.TrustedScript.Fields.AllowedProfiles]?.Value);

            // Parse each referenced script ID
            var scriptIds = scriptFieldValue.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var idStr in scriptIds)
            {
                if (!ID.TryParse(idStr.Trim(), out var scriptItemId))
                {
                    PowerShellLog.Warn($"[Trust] action=invalidScriptId entry={trustItem.Name} id={idStr}");
                    continue;
                }

                Item scriptItem;
                using (new SecurityDisabler())
                {
                    scriptItem = trustItem.Database.GetItem(scriptItemId);
                }

                if (scriptItem == null)
                {
                    PowerShellLog.Warn($"[Trust] action=scriptNotFound entry={trustItem.Name} scriptId={scriptItemId}");
                    continue;
                }

                results.Add(new TrustedScriptEntry(
                    scriptItem.Name, scriptItemId, contentHash: null, ScriptTrustLevel.Trusted,
                    allowTopLevel: false, exports: null, HashMismatchAction.Constrain, allowedProfiles));
            }

            return results;
        }

        private static HashSet<string> ParseAllowedProfiles(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            var profiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var part in value.Split(new[] { ',', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = part.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    profiles.Add(trimmed);
                }
            }

            return profiles.Count > 0 ? profiles : null;
        }

    }

    /// <summary>
    /// Result of evaluating trust for a script.
    /// </summary>
    public class ScriptTrustResult
    {
        public ScriptTrustLevel TrustLevel { get; }
        public bool HashValid { get; }
        public TrustedScriptEntry Entry { get; }

        public ScriptTrustResult(ScriptTrustLevel trustLevel, bool hashValid, TrustedScriptEntry entry)
        {
            TrustLevel = trustLevel;
            HashValid = hashValid;
            Entry = entry;
        }

        /// <summary>
        /// Returns the effective trust level considering hash validity and mismatch action.
        /// </summary>
        public ScriptTrustLevel EffectiveTrustLevel
        {
            get
            {
                if (TrustLevel == ScriptTrustLevel.Untrusted) return ScriptTrustLevel.Untrusted;
                if (HashValid) return TrustLevel;

                // Hash mismatch - determine action
                if (Entry == null) return ScriptTrustLevel.Untrusted;

                switch (Entry.OnHashMismatch)
                {
                    case HashMismatchAction.Warn:
                        return TrustLevel; // Allow but logged
                    case HashMismatchAction.Block:
                    case HashMismatchAction.Constrain:
                    default:
                        return ScriptTrustLevel.Untrusted;
                }
            }
        }
    }
}
