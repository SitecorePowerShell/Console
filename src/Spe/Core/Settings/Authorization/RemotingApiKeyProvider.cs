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

        // Throttle state: key name -> (window start, request count)
        private static readonly ConcurrentDictionary<string, ThrottleState> _throttleState =
            new ConcurrentDictionary<string, ThrottleState>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Returns all enabled API Key items. Used for iterative token validation
        /// when the legacy shared secret doesn't match.
        /// </summary>
        public static List<RemotingApiKey> FindAllEnabled()
        {
            var keys = GetCachedKeys();
            if (keys == null) return null;

            var enabled = new List<RemotingApiKey>();
            foreach (var key in keys)
            {
                if (key.Enabled) enabled.Add(key);
            }

            return enabled.Count > 0 ? enabled : null;
        }

        /// <summary>
        /// Finds an enabled API Key whose shared secret matches the provided value.
        /// Returns null if no match is found.
        /// </summary>
        public static RemotingApiKey FindBySecret(string sharedSecret)
        {
            if (string.IsNullOrEmpty(sharedSecret)) return null;

            var keys = GetCachedKeys();
            if (keys == null) return null;

            foreach (var key in keys)
            {
                if (key.Enabled && SecureCompare.FixedTimeSecretEquals(key.SharedSecret, sharedSecret))
                {
                    return key;
                }
            }

            return null;
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
                        apiKey.RequestLimit - 1, windowEnd);
                }

                state.RequestCount++;
                var remaining = Math.Max(0, apiKey.RequestLimit - state.RequestCount);

                if (state.RequestCount <= apiKey.RequestLimit)
                {
                    return new ThrottleResult(true, apiKey.RequestLimit, remaining, windowEnd);
                }

                PowerShellLog.Warn(
                    $"[ApiKey] action=throttleExceeded key={apiKey.Name} count={state.RequestCount} limit={apiKey.RequestLimit} window={apiKey.ThrottleWindowSeconds}");

                return new ThrottleResult(false, apiKey.RequestLimit, 0, windowEnd);
            }
        }

        public class ThrottleResult
        {
            public static readonly ThrottleResult Unlimited = new ThrottleResult(true, 0, 0, DateTime.MinValue);

            public bool Allowed { get; }
            public int Limit { get; }
            public int Remaining { get; }
            public DateTime WindowResetUtc { get; }

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

            public ThrottleResult(bool allowed, int limit, int remaining, DateTime windowResetUtc)
            {
                Allowed = allowed;
                Limit = limit;
                Remaining = remaining;
                WindowResetUtc = windowResetUtc;
            }
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

                // Warn about duplicate shared secrets
                var secretOwners = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var key in keys)
                {
                    if (!key.Enabled || string.IsNullOrEmpty(key.SharedSecret)) continue;

                    if (secretOwners.TryGetValue(key.SharedSecret, out var existingName))
                    {
                        PowerShellLog.Warn(
                            $"[ApiKey] action=duplicateSecret key={key.Name} duplicateOf={existingName}");
                    }
                    else
                    {
                        secretOwners[key.SharedSecret] = key.Name;
                    }
                }

                PowerShellLog.Info($"[ApiKey] action=registryLoaded count={keys.Count}");
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
                                $"[ApiKey] action=entryLoaded key={key.Name} enabled={key.Enabled} profile={key.Profile ?? "none"}");
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
            var sharedSecret = item.Fields[Templates.RemotingApiKey.Fields.SharedSecret]?.Value?.Trim();
            if (string.IsNullOrEmpty(sharedSecret))
            {
                PowerShellLog.Warn($"[ApiKey] action=noSecret key={item.Name}");
                return null;
            }

            var enabledField = item.Fields[Templates.RemotingApiKey.Fields.Enabled];
            var enabled = enabledField != null && enabledField.Value == "1";

            var profile = item.Fields[Templates.RemotingApiKey.Fields.Profile]?.Value?.Trim();
            var impersonateUser = item.Fields[Templates.RemotingApiKey.Fields.ImpersonateUser]?.Value?.Trim();

            int.TryParse(item.Fields[Templates.RemotingApiKey.Fields.RequestLimit]?.Value, out var requestLimit);
            int.TryParse(item.Fields[Templates.RemotingApiKey.Fields.ThrottleWindow]?.Value, out var throttleWindow);

            return new RemotingApiKey(
                item.Name, sharedSecret, enabled, profile, impersonateUser,
                requestLimit, throttleWindow);
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
