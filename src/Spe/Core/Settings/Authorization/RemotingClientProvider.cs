using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.SecurityModel;
using Spe.Core.Diagnostics;

namespace Spe.Core.Settings.Authorization
{
    /// <summary>
    /// Loads and caches SPE Remoting Client items from the content tree
    /// (both Shared Secret Clients and OAuth Clients). Provides lookup by
    /// access key id for shared-secret auth, by (issuer, client_id) for
    /// OAuth, and per-client throttling.
    ///
    /// Items live under the Security/Remoting Clients node, resolved by item ID.
    /// </summary>
    public static class RemotingClientProvider
    {
        private const string CacheKey = "Spe.RemotingClients";
        private const string AccessKeyIndexCacheKey = "Spe.RemotingClients.ByAccessKeyId";
        private const string KidStatesCacheKey = "Spe.RemotingClients.KidStates";
        private const string IssuerClientIdIndexCacheKey = "Spe.RemotingClients.ByIssuerClientId";

        // Known values returned by GetAuthFailureReason. The "invalid" bucket
        // intentionally collapses "unknown kid" and "bad signature" into a single
        // value to resist enumeration of valid Access Key Ids.
        public const string AuthFailureReasonExpired  = "expired";
        public const string AuthFailureReasonDisabled = "disabled";
        public const string AuthFailureReasonInvalid  = "invalid";

        // Throttle state: key name -> (window start, request count)
        private static readonly ConcurrentDictionary<string, ThrottleState> _throttleState =
            new ConcurrentDictionary<string, ThrottleState>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Returns all enabled Shared Secret Client items.
        /// Sets <paramref name="registryLoaded"/> to true when the registry was
        /// successfully loaded from the database (even if none are enabled).
        /// Returns null when no enabled clients are found or the registry could
        /// not load. Used for iterative token validation when the config-based
        /// shared secret doesn't match.
        /// </summary>
        public static List<RemotingClient> FindAllEnabled(out bool registryLoaded)
        {
            var keys = GetCachedKeys();
            if (keys == null)
            {
                registryLoaded = false;
                return null;
            }

            registryLoaded = true;

            var enabled = new List<RemotingClient>();
            foreach (var key in keys)
            {
                if (key.Enabled) enabled.Add(key);
            }

            return enabled.Count > 0 ? enabled : null;
        }

        public static List<RemotingClient> FindAllEnabled()
        {
            return FindAllEnabled(out _);
        }

        /// <summary>
        /// Finds an enabled Shared Secret Client by its Access Key Id
        /// (O(1) dictionary lookup). Returns null when no match exists, the
        /// matched client is disabled, or the matched client has expired.
        /// </summary>
        public static RemotingClient FindByAccessKeyId(string accessKeyId)
        {
            if (string.IsNullOrEmpty(accessKeyId)) return null;

            var index = GetAccessKeyIndex();
            if (index == null) return null;

            if (index.TryGetValue(accessKeyId, out var key) && key.Enabled && !key.IsExpired)
            {
                return key;
            }

            return null;
        }

        /// <summary>
        /// Finds an enabled OAuth Client whose Allowed Issuer matches
        /// <paramref name="issuer"/> and whose OAuth Client Ids multilist
        /// contains <paramref name="clientId"/>. O(1) dictionary lookup.
        /// Returns null when no match, the matched client is disabled, or
        /// the matched client has expired.
        ///
        /// Lookup is scoped to (issuer, client_id) pairs because client_id
        /// strings are not globally unique across IdPs - tenant A's "app-1"
        /// and tenant B's "app-1" must not cross-match.
        /// </summary>
        public static RemotingClient FindByIssuerAndClientId(string issuer, string clientId)
        {
            if (string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(clientId)) return null;

            var index = GetIssuerClientIdIndex();
            if (index == null) return null;

            var key = MakeIssuerClientIdKey(issuer, clientId);
            if (index.TryGetValue(key, out var client) && client.Enabled && !client.IsExpired)
            {
                return client;
            }

            return null;
        }

        private static string MakeIssuerClientIdKey(string issuer, string clientId)
        {
            // Lowercase issuer to normalise host-case differences (IdPs rarely
            // vary issuer casing, but a single comparison convention is safer).
            // client_id casing is preserved - IdPs do treat it as case-sensitive.
            return (issuer ?? string.Empty).Trim().ToLowerInvariant() + "|" + (clientId ?? string.Empty).Trim();
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
        /// Returns the auth-failure reason for a given Access Key Id.
        /// Known reasons are "expired" and "disabled". Unknown or signature-mismatch
        /// callers should use AuthFailureReasonInvalid directly (this method returns
        /// null for unknown kids so the caller can decide between "invalid" vs no header).
        /// </summary>
        public static string GetAuthFailureReason(string accessKeyId)
        {
            if (string.IsNullOrEmpty(accessKeyId)) return null;

            var states = HttpRuntime.Cache.Get(KidStatesCacheKey) as Dictionary<string, string>;
            if (states == null)
            {
                // Ensure the registry (and the states index alongside it) is built.
                GetCachedKeys();
                states = HttpRuntime.Cache.Get(KidStatesCacheKey) as Dictionary<string, string>;
                if (states == null) return null;
            }

            return states.TryGetValue(accessKeyId, out var reason) ? reason : null;
        }

        /// <summary>
        /// Checks throttle state for the given Remoting Client.
        /// Returns a ThrottleResult with allowed/denied status and rate limit info for headers.
        /// </summary>
        public static ThrottleResult CheckThrottle(RemotingClient client)
        {
            if (!client.HasThrottle)
            {
                return ThrottleResult.Unlimited;
            }

            var now = DateTime.UtcNow;
            var state = _throttleState.GetOrAdd(client.Name, _ => new ThrottleState(now, 0));

            lock (state)
            {
                var windowEnd = state.WindowStart.AddSeconds(client.ThrottleWindowSeconds);
                if (now >= windowEnd)
                {
                    // New window
                    state.WindowStart = now;
                    state.RequestCount = 1;
                    windowEnd = now.AddSeconds(client.ThrottleWindowSeconds);

                    return new ThrottleResult(true, client.RequestLimit,
                        client.RequestLimit - 1, windowEnd, client.ThrottleAction);
                }

                state.RequestCount++;
                var remaining = Math.Max(0, client.RequestLimit - state.RequestCount);

                if (state.RequestCount <= client.RequestLimit)
                {
                    return new ThrottleResult(true, client.RequestLimit, remaining, windowEnd, client.ThrottleAction);
                }

                PowerShellLog.Warn(
                    $"[RemotingClient] action=throttleExceeded remotingClient={client.Name} count={state.RequestCount} limit={client.RequestLimit} window={client.ThrottleWindowSeconds}");

                return new ThrottleResult(false, client.RequestLimit, 0, windowEnd, client.ThrottleAction);
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

        private static Dictionary<string, RemotingClient> GetAccessKeyIndex()
        {
            var cached = HttpRuntime.Cache.Get(AccessKeyIndexCacheKey) as Dictionary<string, RemotingClient>;
            if (cached != null) return cached;

            // Building the index triggers key loading if not already cached
            var keys = GetCachedKeys();
            if (keys == null) return null;

            // Re-check after GetCachedKeys since it builds the index
            return HttpRuntime.Cache.Get(AccessKeyIndexCacheKey) as Dictionary<string, RemotingClient>;
        }

        private static Dictionary<string, RemotingClient> GetIssuerClientIdIndex()
        {
            var cached = HttpRuntime.Cache.Get(IssuerClientIdIndexCacheKey) as Dictionary<string, RemotingClient>;
            if (cached != null) return cached;

            // Building this index also triggers the base load (and the
            // AccessKey + KidStates indexes) when not already cached.
            var keys = GetCachedKeys();
            if (keys == null) return null;

            return HttpRuntime.Cache.Get(IssuerClientIdIndexCacheKey) as Dictionary<string, RemotingClient>;
        }

        private static List<RemotingClient> GetCachedKeys()
        {
            var cached = HttpRuntime.Cache.Get(CacheKey) as List<RemotingClient>;
            if (cached != null) return cached;

            var keys = LoadKeys();
            if (keys == null) return null;

            var ttl = WebServiceSettings.AuthorizationCacheExpirationSecs;
            HttpRuntime.Cache.Insert(CacheKey, keys, null,
                DateTime.UtcNow.AddSeconds(ttl),
                System.Web.Caching.Cache.NoSlidingExpiration);

            // Build Access Key Id index alongside the key list
            var index = new Dictionary<string, RemotingClient>(StringComparer.OrdinalIgnoreCase);
            // Parallel kid-states index so the handler can distinguish expired/disabled
            // from "unknown kid" when emitting X-SPE-AuthFailureReason on 401 responses.
            var kidStates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in keys)
            {
                if (string.IsNullOrEmpty(key.AccessKeyId)) continue;

                if (index.ContainsKey(key.AccessKeyId))
                {
                    PowerShellLog.Warn(
                        $"[RemotingClient] action=duplicateAccessKeyId accessKeyId={key.AccessKeyId} remotingClient={key.Name} duplicateOf={index[key.AccessKeyId].Name}");
                }
                else
                {
                    index[key.AccessKeyId] = key;
                }

                // Record non-usable states. Expired takes precedence over Disabled so
                // an operator sees the more informative reason first.
                if (!kidStates.ContainsKey(key.AccessKeyId))
                {
                    if (key.IsExpired)
                    {
                        kidStates[key.AccessKeyId] = AuthFailureReasonExpired;
                    }
                    else if (!key.Enabled)
                    {
                        kidStates[key.AccessKeyId] = AuthFailureReasonDisabled;
                    }
                }
            }

            HttpRuntime.Cache.Insert(AccessKeyIndexCacheKey, index, null,
                DateTime.UtcNow.AddSeconds(ttl),
                System.Web.Caching.Cache.NoSlidingExpiration);

            HttpRuntime.Cache.Insert(KidStatesCacheKey, kidStates, null,
                DateTime.UtcNow.AddSeconds(ttl),
                System.Web.Caching.Cache.NoSlidingExpiration);

            // OAuth Client items live in the same folder as Shared Secret
            // Client items. Build a parallel (issuer, client_id) index for
            // the OAuth provider's lookup path.
            var oauthEntries = LoadOAuthClients();
            var issuerClientIdIndex = new Dictionary<string, RemotingClient>(StringComparer.OrdinalIgnoreCase);
            if (oauthEntries != null)
            {
                foreach (var entry in oauthEntries)
                {
                    foreach (var cid in entry.ClientIds)
                    {
                        var indexKey = MakeIssuerClientIdKey(entry.Issuer, cid);
                        if (issuerClientIdIndex.ContainsKey(indexKey))
                        {
                            PowerShellLog.Warn(
                                $"[RemotingClient] action=duplicateIssuerClientId issuer={LogSanitizer.SanitizeValue(entry.Issuer)} clientId={LogSanitizer.SanitizeValue(cid)} remotingClient={entry.Client.Name} duplicateOf={issuerClientIdIndex[indexKey].Name}");
                        }
                        else
                        {
                            issuerClientIdIndex[indexKey] = entry.Client;
                        }
                    }
                }
            }

            HttpRuntime.Cache.Insert(IssuerClientIdIndexCacheKey, issuerClientIdIndex, null,
                DateTime.UtcNow.AddSeconds(ttl),
                System.Web.Caching.Cache.NoSlidingExpiration);

            return keys;
        }

        private sealed class OAuthClientEntry
        {
            public RemotingClient Client { get; set; }
            public string Issuer { get; set; }
            public List<string> ClientIds { get; set; }
        }

        private static List<OAuthClientEntry> LoadOAuthClients()
        {
            try
            {
                var db = Factory.GetDatabase(ApplicationSettings.ScriptLibraryDb);
                if (db == null) return null;

                Item settingsFolder;
                using (new SecurityDisabler())
                {
                    settingsFolder = db.GetItem(ItemIDs.RemotingClients);
                }
                if (settingsFolder == null) return null;

                var entries = new List<OAuthClientEntry>();
                using (new SecurityDisabler())
                {
                    CollectOAuthClientsRecursive(settingsFolder, entries);
                }

                PowerShellLog.Debug($"[RemotingClient] action=oauthRegistryLoaded count={entries.Count}");
                return entries;
            }
            catch (Exception ex)
            {
                PowerShellLog.Error("[RemotingClient] action=oauthLoadFailed", ex);
                return null;
            }
        }

        private static void CollectOAuthClientsRecursive(Item folder, List<OAuthClientEntry> entries)
        {
            foreach (Item child in folder.GetChildren())
            {
                if (child.TemplateID == Templates.OAuthClient.Id)
                {
                    try
                    {
                        var entry = ParseOAuthClient(child);
                        if (entry != null) entries.Add(entry);
                    }
                    catch (Exception ex)
                    {
                        PowerShellLog.Error($"[RemotingClient] action=oauthEntryLoadFailed remotingClient={child.Name}", ex);
                    }
                }
                else if (child.HasChildren)
                {
                    CollectOAuthClientsRecursive(child, entries);
                }
            }
        }

        private static OAuthClientEntry ParseOAuthClient(Item item)
        {
            var issuer = item.Fields[Templates.OAuthClient.Fields.AllowedIssuer]?.Value?.Trim();
            if (string.IsNullOrEmpty(issuer))
            {
                PowerShellLog.Warn($"[RemotingClient] action=noAllowedIssuer remotingClient={item.Name}");
                return null;
            }

            var rawClientIds = item.Fields[Templates.OAuthClient.Fields.OAuthClientIds]?.Value ?? string.Empty;
            var clientIds = rawClientIds
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (clientIds.Count == 0)
            {
                PowerShellLog.Warn($"[RemotingClient] action=noOAuthClientIds remotingClient={item.Name}");
                return null;
            }

            var enabledField = item.Fields[Templates.RemotingClient.Fields.Enabled];
            var enabled = enabledField != null && enabledField.Value == "1";

            string policy = null;
            var policyFieldValue = item.Fields[Templates.RemotingClient.Fields.Policy]?.Value?.Trim();
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
                    PowerShellLog.Warn($"[RemotingClient] action=policyItemMissing remotingClient={item.Name} policyId={policyId}");
                }
            }

            var impersonateUser = item.Fields[Templates.RemotingClient.Fields.ImpersonateUser]?.Value?.Trim();
            if (enabled && string.IsNullOrEmpty(impersonateUser))
            {
                PowerShellLog.Warn($"[RemotingClient] action=noImpersonateUser remotingClient={item.Name}");
            }

            int.TryParse(item.Fields[Templates.RemotingClient.Fields.RequestLimit]?.Value, out var requestLimit);
            int.TryParse(item.Fields[Templates.RemotingClient.Fields.ThrottleWindow]?.Value, out var throttleWindow);
            var throttleAction = item.Fields[Templates.RemotingClient.Fields.ThrottleAction]?.Value?.Trim();

            DateTime? expires = null;
            var expiresField = item.Fields[Templates.RemotingClient.Fields.Expires];
            if (expiresField != null && !string.IsNullOrEmpty(expiresField.Value))
            {
                var expiresDate = Sitecore.DateUtil.IsoDateToDateTime(expiresField.Value, DateTime.MinValue);
                if (expiresDate != DateTime.MinValue) expires = expiresDate;
            }

            // OAuth Client items don't carry AccessKeyId / SharedSecret; leave those empty.
            var client = new RemotingClient(
                item.Name, "", "", enabled, policy, impersonateUser,
                requestLimit, throttleWindow, throttleAction, expires);

            return new OAuthClientEntry { Client = client, Issuer = issuer, ClientIds = clientIds };
        }

        private static List<RemotingClient> LoadKeys()
        {
            try
            {
                var db = Factory.GetDatabase(ApplicationSettings.ScriptLibraryDb);
                if (db == null) return null;

                Item settingsFolder;
                using (new SecurityDisabler())
                {
                    settingsFolder = db.GetItem(ItemIDs.RemotingClients);
                }

                if (settingsFolder == null)
                {
                    PowerShellLog.Debug("[RemotingClient] action=folderNotFound");
                    return null;
                }

                var keys = new List<RemotingClient>();
                using (new SecurityDisabler())
                {
                    CollectSharedSecretClientsRecursive(settingsFolder, keys);
                }

                PowerShellLog.Debug($"[RemotingClient] action=registryLoaded count={keys.Count}");
                return keys;
            }
            catch (Exception ex)
            {
                PowerShellLog.Error("[RemotingClient] action=loadFailed", ex);
                return null;
            }
        }

        private static void CollectSharedSecretClientsRecursive(Item folder, List<RemotingClient> keys)
        {
            foreach (Item child in folder.GetChildren())
            {
                if (child.TemplateID == Templates.SharedSecretClient.Id)
                {
                    try
                    {
                        var key = ParseSharedSecretClient(child);
                        if (key != null)
                        {
                            keys.Add(key);
                            PowerShellLog.Debug(
                                $"[RemotingClient] action=entryLoaded remotingClient={key.Name} enabled={key.Enabled} profile={key.Policy ?? "none"}");
                        }
                    }
                    catch (Exception ex)
                    {
                        PowerShellLog.Error($"[RemotingClient] action=entryLoadFailed remotingClient={child.Name}", ex);
                    }
                }
                else if (child.HasChildren)
                {
                    CollectSharedSecretClientsRecursive(child, keys);
                }
            }
        }

        private static RemotingClient ParseSharedSecretClient(Item item)
        {
            var accessKeyId = item.Fields[Templates.SharedSecretClient.Fields.AccessKeyId]?.Value?.Trim();
            if (string.IsNullOrEmpty(accessKeyId))
            {
                PowerShellLog.Warn($"[RemotingClient] action=noAccessKeyId remotingClient={item.Name}");
            }

            var sharedSecret = item.Fields[Templates.SharedSecretClient.Fields.SharedSecret]?.Value?.Trim();
            if (string.IsNullOrEmpty(sharedSecret))
            {
                PowerShellLog.Warn($"[RemotingClient] action=noSecret remotingClient={item.Name}");
                return null;
            }

            if (sharedSecret.Length < 32)
            {
                PowerShellLog.Warn($"[RemotingClient] action=secretTooShort remotingClient={item.Name} length={sharedSecret.Length} minimum=32");
            }

            var enabledField = item.Fields[Templates.RemotingClient.Fields.Enabled];
            var enabled = enabledField != null && enabledField.Value == "1";

            // Policy field is a Droplink (stores item ID). Resolve to the policy item name.
            // If the Droplink references a deleted/missing item, use a sentinel name
            // that won't match any policy — RemotingPolicyManager.ResolvePolicy will DenyAll.
            string policy = null;
            var policyFieldValue = item.Fields[Templates.RemotingClient.Fields.Policy]?.Value?.Trim();
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
                    PowerShellLog.Warn($"[RemotingClient] action=policyItemMissing remotingClient={item.Name} policyId={policyId}");
                }
            }
            var impersonateUser = item.Fields[Templates.RemotingClient.Fields.ImpersonateUser]?.Value?.Trim();
            if (enabled && string.IsNullOrEmpty(impersonateUser))
            {
                PowerShellLog.Warn($"[RemotingClient] action=noImpersonateUser remotingClient={item.Name}");
            }

            int.TryParse(item.Fields[Templates.RemotingClient.Fields.RequestLimit]?.Value, out var requestLimit);
            int.TryParse(item.Fields[Templates.RemotingClient.Fields.ThrottleWindow]?.Value, out var throttleWindow);
            var throttleAction = item.Fields[Templates.RemotingClient.Fields.ThrottleAction]?.Value?.Trim();

            DateTime? expires = null;
            var expiresField = item.Fields[Templates.RemotingClient.Fields.Expires];
            if (expiresField != null && !string.IsNullOrEmpty(expiresField.Value))
            {
                var expiresDate = Sitecore.DateUtil.IsoDateToDateTime(expiresField.Value, DateTime.MinValue);
                if (expiresDate != DateTime.MinValue)
                {
                    expires = expiresDate;
                    if (DateTime.UtcNow > expiresDate)
                    {
                        // Keep the entry in the parsed list with its Expires set. The
                        // state-of-record is the RemotingClient.IsExpired property;
                        // FindByAccessKeyId excludes IsExpired so expired keys never
                        // authenticate. They remain visible to GetAuthFailureReason.
                        PowerShellLog.Info($"[RemotingClient] action=expired remotingClient={item.Name} expires={expiresDate:O}");
                    }
                }
            }

            return new RemotingClient(
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
