using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Sitecore.Configuration;
using Sitecore.Data;
using Spe.Core.Diagnostics;

namespace Spe.Core.Settings.Authorization
{
    /// <summary>
    /// Registry of trusted scripts loaded from Spe.config.
    /// Provides O(1) lookup by Sitecore item GUID with content hash verification.
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
        public static ScriptTrustResult EvaluateTrust(ID itemId, string scriptBody)
        {
            EnsureInitialized();

            if (!_entriesById.TryGetValue(itemId, out var entry))
            {
                return new ScriptTrustResult(ScriptTrustLevel.Untrusted, true, null);
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
                        $"ScriptTrustRegistry: content hash mismatch for trusted script '{entry.Name}' (ItemId={itemId}). " +
                        $"Expected={entry.ContentHash}, Actual={actualHash}, Action={entry.OnHashMismatch}");
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

        private static void EnsureInitialized()
        {
            if (_initialized) return;
            lock (_lock)
            {
                if (_initialized) return;
                LoadTrustedScripts();
                _initialized = true;
            }
        }

        private static void LoadTrustedScripts()
        {
            var configNode = Factory.GetConfigNode("powershell/trustedScripts");
            if (configNode == null) return;

            foreach (XmlNode child in configNode.ChildNodes)
            {
                var element = child as XmlElement;
                if (element == null || element.Name != "script") continue;

                var name = element.GetAttribute("name");
                if (string.IsNullOrEmpty(name)) continue;

                try
                {
                    var entry = ParseEntry(element, name);
                    if (entry != null)
                    {
                        _entriesById[entry.ItemId] = entry;
                        _entriesByName[entry.Name] = entry;
                        PowerShellLog.Info($"Registered trusted script: {name} (ItemId={entry.ItemId}, Trust={entry.Trust})");
                    }
                }
                catch (Exception ex)
                {
                    PowerShellLog.Error($"Failed to load trusted script entry '{name}'.", ex);
                }
            }
        }

        private static TrustedScriptEntry ParseEntry(XmlElement element, string name)
        {
            // Item ID (required)
            var itemIdStr = element.GetAttribute("itemId");
            if (string.IsNullOrEmpty(itemIdStr) || !ID.TryParse(itemIdStr, out var itemId))
            {
                PowerShellLog.Warn($"ScriptTrustRegistry: skipping entry '{name}' - invalid or missing itemId.");
                return null;
            }

            // Content hash (optional)
            var contentHash = element.GetAttribute("contentHash");

            // Trust level
            var trustLevel = ScriptTrustLevel.Trusted;
            var trustAttr = element.GetAttribute("trust");
            if (!string.IsNullOrEmpty(trustAttr))
            {
                Enum.TryParse(trustAttr, true, out trustLevel);
            }

            // Allow top-level code
            var allowTopLevel = false;
            var topLevelAttr = element.GetAttribute("allowTopLevel");
            if (!string.IsNullOrEmpty(topLevelAttr))
            {
                bool.TryParse(topLevelAttr, out allowTopLevel);
            }

            // Hash mismatch action
            var onHashMismatch = HashMismatchAction.Constrain;
            var mismatchAttr = element.GetAttribute("onHashMismatch");
            if (!string.IsNullOrEmpty(mismatchAttr))
            {
                Enum.TryParse(mismatchAttr, true, out onHashMismatch);
            }

            // Exported functions
            var exports = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var exportsNode = element.SelectSingleNode("exports");
            if (exportsNode != null)
            {
                foreach (XmlNode funcNode in exportsNode.SelectNodes("function"))
                {
                    var funcName = funcNode.InnerText?.Trim();
                    if (!string.IsNullOrEmpty(funcName))
                    {
                        exports.Add(funcName);
                    }
                }
            }

            return new TrustedScriptEntry(name, itemId, contentHash, trustLevel, allowTopLevel, exports, onHashMismatch);
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
