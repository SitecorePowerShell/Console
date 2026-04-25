using System;
using System.Collections.Generic;
using System.Web;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.SecurityModel;
using Spe.Core.Diagnostics;

namespace Spe.Core.Settings.Authorization
{
    /// <summary>
    /// Loads and manages remoting policies from Sitecore content items.
    /// Policies are defined under the Security/Policies node, resolved by item ID.
    /// </summary>
    public static class RemotingPolicyManager
    {

        private const string CacheKey = "Spe.RemotingPolicies";

        /// <summary>
        /// Returns true if a policy with the given name exists.
        /// </summary>
        public static bool PolicyExists(string policyName)
        {
            if (string.IsNullOrEmpty(policyName)) return false;
            var policies = GetCachedPolicies();
            return policies != null && policies.ContainsKey(policyName);
        }

        /// <summary>
        /// Gets a policy by name. Returns null if not found.
        /// </summary>
        public static RemotingPolicy GetPolicy(string policyName)
        {
            if (string.IsNullOrEmpty(policyName)) return null;
            var policies = GetCachedPolicies();
            if (policies == null) return null;
            policies.TryGetValue(policyName, out var policy);
            return policy;
        }

        /// <summary>
        /// Resolves the effective policy for a request.
        /// Remoting Clients without a policy are denied.
        /// </summary>
        public static RemotingPolicy ResolvePolicy(string clientPolicy)
        {
            if (!string.IsNullOrEmpty(clientPolicy))
            {
                var resolved = GetPolicy(clientPolicy);
                if (resolved != null) return resolved;

                PowerShellLog.Error(
                    $"[Policy] action=unknownPolicy source=remotingClient policy={clientPolicy}");
                return RemotingPolicy.DenyAll;
            }

            PowerShellLog.Error("[Policy] action=noPolicy reason=Remoting Client has no policy assigned");
            return RemotingPolicy.DenyAll;
        }

        /// <summary>
        /// Invalidates the policy cache so policies will be reloaded on next access.
        /// </summary>
        public static void Invalidate()
        {
            HttpRuntime.Cache.Remove(CacheKey);
        }

        private static Dictionary<string, RemotingPolicy> GetCachedPolicies()
        {
            var cached = HttpRuntime.Cache.Get(CacheKey) as Dictionary<string, RemotingPolicy>;
            if (cached != null) return cached;

            var policies = LoadPolicies();

            var ttl = WebServiceSettings.AuthorizationCacheExpirationSecs;
            HttpRuntime.Cache.Insert(CacheKey, policies, null,
                DateTime.UtcNow.AddSeconds(ttl),
                System.Web.Caching.Cache.NoSlidingExpiration);

            return policies;
        }

        private static Dictionary<string, RemotingPolicy> LoadPolicies()
        {
            try
            {
                var db = Factory.GetDatabase(ApplicationSettings.ScriptLibraryDb);
                if (db == null)
                {
                    PowerShellLog.Warn("[Policy] action=databaseNull");
                    return new Dictionary<string, RemotingPolicy>(StringComparer.OrdinalIgnoreCase);
                }

                Item policiesFolder;
                using (new SecurityDisabler())
                {
                    policiesFolder = db.GetItem(ItemIDs.Policies);
                }

                if (policiesFolder == null)
                {
                    PowerShellLog.Debug("[Policy] action=folderNotFound");
                    return new Dictionary<string, RemotingPolicy>(StringComparer.OrdinalIgnoreCase);
                }

                var policies = new Dictionary<string, RemotingPolicy>(StringComparer.OrdinalIgnoreCase);
                using (new SecurityDisabler())
                {
                    CollectPoliciesRecursive(policiesFolder, policies);
                }

                PowerShellLog.Debug($"[Policy] action=registryLoaded count={policies.Count}");
                return policies;
            }
            catch (Exception ex)
            {
                PowerShellLog.Error("[Policy] action=loadFailed", ex);
                return new Dictionary<string, RemotingPolicy>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private static void CollectPoliciesRecursive(Item folder, Dictionary<string, RemotingPolicy> policies)
        {
            foreach (Item child in folder.GetChildren())
            {
                if (child.TemplateID == Templates.RemotingPolicy.Id)
                {
                    try
                    {
                        var policy = ParsePolicy(child);
                        if (policy != null)
                        {
                            policies[policy.Name] = policy;
                            PowerShellLog.Debug(
                                $"[Policy] action=entryLoaded policy={policy.Name} fullLanguage={policy.FullLanguage} restrictCommands={policy.RestrictCommands}");
                        }
                    }
                    catch (Exception ex)
                    {
                        PowerShellLog.Error($"[Policy] action=entryLoadFailed item={child.Name}", ex);
                    }
                }
                else if (child.HasChildren)
                {
                    CollectPoliciesRecursive(child, policies);
                }
            }
        }

        /// <summary>
        /// Parses a Sitecore item into a <see cref="RemotingPolicy"/>.
        /// Use this when you have an item reference and want the policy as it is
        /// on disk, bypassing the by-name cache. Needed in the ISE where multiple
        /// policies with the same name may exist under different subfolders.
        /// </summary>
        public static RemotingPolicy GetPolicyFromItem(Item item)
        {
            if (item == null) return null;
            if (item.TemplateID != Templates.RemotingPolicy.Id) return null;
            return ParsePolicy(item);
        }

        /// <summary>
        /// Resolves a policy item by its ID string in the script library database.
        /// Returns null if the ID is blank, unparseable, points at a missing item, or the
        /// item is not a Remoting Policy. Runs under <see cref="SecurityDisabler"/> so the
        /// caller doesn't need to manage read access.
        /// </summary>
        public static Item ResolvePolicyItem(string idStr)
        {
            if (string.IsNullOrEmpty(idStr) || !ID.TryParse(idStr, out var id)) return null;
            var db = Factory.GetDatabase(ApplicationSettings.ScriptLibraryDb);
            if (db == null) return null;
            using (new SecurityDisabler())
            {
                var item = db.GetItem(id);
                return item != null && item.TemplateID == Templates.RemotingPolicy.Id ? item : null;
            }
        }

        private static RemotingPolicy ParsePolicy(Item item)
        {
            var fullLanguageField = item.Fields[Templates.RemotingPolicy.Fields.FullLanguage];
            var fullLanguage = fullLanguageField != null && fullLanguageField.Value == "1";

            // Parse allowed commands (multi-line text, one per line)
            // When Full Language is unchecked (CLM), commands are always restricted.
            // An empty allowlist under CLM means no inline commands are permitted.
            var allowedCommandsText = item.Fields[Templates.RemotingPolicy.Fields.AllowedCommands]?.Value;
            var restrictCommands = !fullLanguage;
            var allowedCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrEmpty(allowedCommandsText))
            {
                var lines = allowedCommandsText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var cmd = line.Trim();
                    if (!string.IsNullOrEmpty(cmd))
                    {
                        allowedCommands.Add(cmd);
                    }
                }
                if (allowedCommands.Count > 0)
                {
                    restrictCommands = true;
                }
            }

            // Parse approved scripts (Treelist — pipe-delimited item IDs)
            var approvedScriptsText = item.Fields[Templates.RemotingPolicy.Fields.ApprovedScripts]?.Value;
            var approvedScriptIds = new HashSet<ID>();
            if (!string.IsNullOrEmpty(approvedScriptsText))
            {
                var ids = approvedScriptsText.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var idStr in ids)
                {
                    if (ID.TryParse(idStr.Trim(), out var id))
                    {
                        approvedScriptIds.Add(id);
                    }
                }
            }

            // Parse audit level
            var auditLevel = AuditLevel.Violations;
            var auditLevelText = item.Fields[Templates.RemotingPolicy.Fields.AuditLevel]?.Value?.Trim();
            if (!string.IsNullOrEmpty(auditLevelText))
            {
                if (!Enum.TryParse(auditLevelText, true, out auditLevel))
                {
                    PowerShellLog.Warn($"[Policy] action=auditLevelParseFailed policy={item.Name} value=\"{auditLevelText}\" fallback=Violations");
                    auditLevel = AuditLevel.Violations;
                }
            }

            return new RemotingPolicy(
                item.Name, fullLanguage, restrictCommands, allowedCommands, approvedScriptIds, auditLevel);
        }
    }
}
