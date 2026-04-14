using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Web;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.SecurityModel;
using Spe.Core.Diagnostics;

namespace Spe.Core.Settings.Authorization
{
    /// <summary>
    /// Loads and caches SPE Remoting API Key items from the content tree.
    /// Provides lookup by shared secret for authentication and per-key throttling.
    ///
    /// API Key items live under:
    /// /sitecore/system/Modules/PowerShell/Settings/Remoting/API Keys/
    /// </summary>
    public static class RemotingApiKeyProvider
    {
        private const string SettingsPath =
            "/sitecore/system/Modules/PowerShell/Settings/Remoting/API Keys";
        private const string CacheKey = "Spe.RemotingApiKeys";
        private const string AccessKeyIndexCacheKey = "Spe.RemotingApiKeys.ByAccessKeyId";

        // Throttle state: key name -> (window start, request count)
        private static readonly ConcurrentDictionary<string, ThrottleState> _throttleState =
            new ConcurrentDictionary<string, ThrottleState>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Returns all enabled API Key items. Used for iterative token validation
        /// when the legacy shared secret doesn't match.
        /// </summary>
        /// <summary>
        /// Returns all enabled API Key items.
        /// Sets <paramref name="registryLoaded"/> to true when the registry was
        /// successfully loaded from the database (even if no enabled keys exist).
        /// Returns null when no enabled keys are found or the registry could not load.
        /// </summary>
        public static List<RemotingApiKey> FindAllEnabled(out bool registryLoaded)
        {
            var keys = GetCachedKeys();
            if (keys == null)
            {
                registryLoaded = false;
                return null;
            }

            registryLoaded = true;

            var enabled = new List<RemotingApiKey>();
            foreach (var key in keys)
            {
                if (key.Enabled) enabled.Add(key);
            }

            return enabled.Count > 0 ? enabled : null;
        }

        public static List<RemotingApiKey> FindAllEnabled()
        {
            return FindAllEnabled(out _);
        }

        /// <summary>
        /// Finds an enabled API Key by its Access Key Id (O(1) dictionary lookup).
        /// Returns null if no match is found or the matched key is disabled.
        /// </summary>
        public static RemotingApiKey FindByAccessKeyId(string accessKeyId)
        {
            if (string.IsNullOrEmpty(accessKeyId)) return null;

            var index = GetAccessKeyIndex();
            if (index == null) return null;

            if (index.TryGetValue(accessKeyId, out var key) && key.Enabled)
            {
                return key;
            }

            return null;
        }

        /// <summary>
        /// Checks whether the Access Key Id index was successfully loaded.
        /// Returns false during cold start when the database is not yet available.
        /// </summary>
        public static bool IsRegistryLoaded()
        {
            return GetAccessKeyIndex() != null;
        }

        /// <summary>
        /// Checks throttle state for the given API Key.
        /// Returns a ThrottleResult with allowed/denied status and rate limit info for headers.
        /// </summary>
        public static ThrottleResult CheckThrottle(RemotingApiKey apiKey)
        {
            if (!apiKey.HasThrottle)
            {
                return ThrottleResult.Unlimited;
            }

            var now = DateTime.UtcNow;
            var state = _throttleState.GetOrAdd(apiKey.Name, _ => new ThrottleState(now, 0));

            lock (state)
            {
                var windowEnd = state.WindowStart.AddSeconds(apiKey.ThrottleWindowSeconds);
                if (now >= windowEnd)
                {
                    // New window
                    state.WindowStart = now;
                    state.RequestCount = 1;
                    windowEnd = now.AddSeconds(apiKey.ThrottleWindowSeconds);

                    return new ThrottleResult(true, apiKey.RequestLimit,
                        apiKey.RequestLimit - 1, windowEnd, apiKey.ThrottleAction);
                }

                state.RequestCount++;
                var remaining = Math.Max(0, apiKey.RequestLimit - state.RequestCount);

                if (state.RequestCount <= apiKey.RequestLimit)
                {
                    return new ThrottleResult(true, apiKey.RequestLimit, remaining, windowEnd, apiKey.ThrottleAction);
                }

                PowerShellLog.Warn(
                    $"[ApiKey] action=throttleExceeded key={apiKey.Name} count={state.RequestCount} limit={apiKey.RequestLimit} window={apiKey.ThrottleWindowSeconds}");

                return new ThrottleResult(false, apiKey.RequestLimit, 0, windowEnd, apiKey.ThrottleAction);
            }
        }

        public class ThrottleResult
        {
            public static readonly ThrottleResult Unlimited = new ThrottleResult(true, 0, 0, DateTime.MinValue, "Block");

            public bool Allowed { get; }
            public int Limit { get; }
            public int Remaining { get; }
            public DateTime WindowResetUtc { get; }
            public string Action { get; }

            public bool HasLimit => Limit > 0;

            public int RetryAfterSeconds
            {
                get
                {
                    if (Allowed || WindowResetUtc == DateTime.MinValue) return 0;
                    return Math.Max(1, (int)Math.Ceiling((WindowResetUtc - DateTime.UtcNow).TotalSeconds));
                }
            }

            public long ResetUnixTimestamp
            {
                get
                {
                    if (WindowResetUtc == DateTime.MinValue) return 0;
                    return (long)(WindowResetUtc - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                }
            }

            public ThrottleResult(bool allowed, int limit, int remaining, DateTime windowResetUtc, string action)
            {
                Allowed = allowed;
                Limit = limit;
                Remaining = remaining;
                WindowResetUtc = windowResetUtc;
                Action = action;
            }
        }

        private static Dictionary<string, RemotingApiKey> GetAccessKeyIndex()
        {
            var cached = HttpRuntime.Cache.Get(AccessKeyIndexCacheKey) as Dictionary<string, RemotingApiKey>;
            if (cached != null) return cached;

            // Building the index triggers key loading if not already cached
            var keys = GetCachedKeys();
            if (keys == null) return null;

            // Re-check after GetCachedKeys since it builds the index
            return HttpRuntime.Cache.Get(AccessKeyIndexCacheKey) as Dictionary<string, RemotingApiKey>;
        }

        private static List<RemotingApiKey> GetCachedKeys()
        {
            var cached = HttpRuntime.Cache.Get(CacheKey) as List<RemotingApiKey>;
            if (cached != null) return cached;

            var keys = LoadKeys();
            if (keys == null) return null;

            var ttl = WebServiceSettings.AuthorizationCacheExpirationSecs;
            HttpRuntime.Cache.Insert(CacheKey, keys, null,
                DateTime.UtcNow.AddSeconds(ttl),
                System.Web.Caching.Cache.NoSlidingExpiration);

            // Build Access Key Id index alongside the key list
            var index = new Dictionary<string, RemotingApiKey>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in keys)
            {
                if (string.IsNullOrEmpty(key.AccessKeyId)) continue;

                if (index.ContainsKey(key.AccessKeyId))
                {
                    PowerShellLog.Warn(
                        $"[ApiKey] action=duplicateAccessKeyId accessKeyId={key.AccessKeyId} key={key.Name} duplicateOf={index[key.AccessKeyId].Name}");
                }
                else
                {
                    index[key.AccessKeyId] = key;
                }
            }

            HttpRuntime.Cache.Insert(AccessKeyIndexCacheKey, index, null,
                DateTime.UtcNow.AddSeconds(ttl),
                System.Web.Caching.Cache.NoSlidingExpiration);

            return keys;
        }

        private static List<RemotingApiKey> LoadKeys()
        {
            try
            {
                var db = Factory.GetDatabase(ApplicationSettings.ScriptLibraryDb);
                if (db == null) return null;

                Item settingsFolder;
                using (new SecurityDisabler())
                {
                    settingsFolder = db.GetItem(SettingsPath);
                }

                if (settingsFolder == null)
                {
                    PowerShellLog.Debug("[ApiKey] action=folderNotFound");
                    return null;
                }

                var keys = new List<RemotingApiKey>();
                using (new SecurityDisabler())
                {
                    CollectApiKeysRecursive(settingsFolder, keys);
                }

                PowerShellLog.Debug($"[ApiKey] action=registryLoaded count={keys.Count}");
                return keys;
            }
            catch (Exception ex)
            {
                PowerShellLog.Error("[ApiKey] action=loadFailed", ex);
                return null;
            }
        }

        private static void CollectApiKeysRecursive(Item folder, List<RemotingApiKey> keys)
        {
            foreach (Item child in folder.GetChildren())
            {
                if (child.TemplateID == Templates.RemotingApiKey.Id)
                {
                    try
                    {
                        var key = ParseApiKey(child);
                        if (key != null)
                        {
                            keys.Add(key);
                            PowerShellLog.Debug(
                                $"[ApiKey] action=entryLoaded key={key.Name} enabled={key.Enabled} profile={key.Policy ?? "none"}");
                        }
                    }
                    catch (Exception ex)
                    {
                        PowerShellLog.Error($"[ApiKey] action=entryLoadFailed key={child.Name}", ex);
                    }
                }
                else if (child.HasChildren)
                {
                    CollectApiKeysRecursive(child, keys);
                }
            }
        }

        private static RemotingApiKey ParseApiKey(Item item)
        {
            var accessKeyId = item.Fields[Templates.RemotingApiKey.Fields.AccessKeyId]?.Value?.Trim();
            if (string.IsNullOrEmpty(accessKeyId))
            {
                PowerShellLog.Warn($"[ApiKey] action=noAccessKeyId key={item.Name}");
            }

            var sharedSecret = item.Fields[Templates.RemotingApiKey.Fields.SharedSecret]?.Value?.Trim();
            if (string.IsNullOrEmpty(sharedSecret))
            {
                PowerShellLog.Warn($"[ApiKey] action=noSecret key={item.Name}");
                return null;
            }

            if (sharedSecret.Length < 32)
            {
                PowerShellLog.Warn($"[ApiKey] action=secretTooShort key={item.Name} length={sharedSecret.Length} minimum=32");
            }

            var enabledField = item.Fields[Templates.RemotingApiKey.Fields.Enabled];
            var enabled = enabledField != null && enabledField.Value == "1";

            // Policy field is a Droplink (stores item ID). Resolve to the policy item name.
            // If the Droplink references a deleted/missing item, use a sentinel name
            // that won't match any policy — RemotingPolicyManager.ResolvePolicy will DenyAll.
            string policy = null;
            var policyFieldValue = item.Fields[Templates.RemotingApiKey.Fields.Policy]?.Value?.Trim();
            if (!string.IsNullOrEmpty(policyFieldValue) && Sitecore.Data.ID.TryParse(policyFieldValue, out var policyId))
            {
                var policyItem = item.Database.GetItem(policyId);
                if (policyItem != null)
                {
                    policy = policyItem.Name;
                }
                else
                {
                    policy = $"__deleted:{policyId}";
                    PowerShellLog.Warn($"[ApiKey] action=policyItemMissing key={item.Name} policyId={policyId}");
                }
            }
            var impersonateUser = item.Fields[Templates.RemotingApiKey.Fields.ImpersonateUser]?.Value?.Trim();
            if (enabled && string.IsNullOrEmpty(impersonateUser))
            {
                PowerShellLog.Warn($"[ApiKey] action=noImpersonateUser key={item.Name}");
            }

            int.TryParse(item.Fields[Templates.RemotingApiKey.Fields.RequestLimit]?.Value, out var requestLimit);
            int.TryParse(item.Fields[Templates.RemotingApiKey.Fields.ThrottleWindow]?.Value, out var throttleWindow);
            var throttleAction = item.Fields[Templates.RemotingApiKey.Fields.ThrottleAction]?.Value?.Trim();

            DateTime? expires = null;
            var expiresField = item.Fields[Templates.RemotingApiKey.Fields.Expires];
            if (expiresField != null && !string.IsNullOrEmpty(expiresField.Value))
            {
                var expiresDate = Sitecore.DateUtil.IsoDateToDateTime(expiresField.Value, DateTime.MinValue);
                if (expiresDate != DateTime.MinValue)
                {
                    expires = expiresDate;
                    if (DateTime.UtcNow > expiresDate)
                    {
                        PowerShellLog.Info($"[ApiKey] action=expired key={item.Name} expires={expiresDate:O}");
                        return null;
                    }
                }
            }

            return new RemotingApiKey(
                item.Name, accessKeyId, sharedSecret, enabled, policy, impersonateUser,
                requestLimit, throttleWindow, throttleAction, expires);
        }

        private class ThrottleState
        {
            public DateTime WindowStart;
            public int RequestCount;

            public ThrottleState(DateTime windowStart, int requestCount)
            {
                WindowStart = windowStart;
                RequestCount = requestCount;
            }
        }
    }
}
