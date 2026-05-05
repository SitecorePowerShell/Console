using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Caching;
using Spe.Core.Diagnostics;
using Spe.Core.Extensions;
using Spe.Core.Settings;

namespace Spe.Core.Host
{
    public static class ScriptSessionManager
    {
        private const string sessionIdPrefix = "$scriptSession$";
        private const string expirationSetting = "Spe.PersistentSessionExpirationMinutes";
        private const string preventSessionCleanupSetting = "Spe.PreventPersistentSessionCleanup";

        // Lock-free read index of every live cache key. The wait endpoint
        // probes this twice per loop iteration and cmdlets enumerate it for
        // session listings; backing those reads with a ConcurrentDictionary
        // lets them proceed without blocking each other. The byte value is
        // unused - ConcurrentDictionary is borrowed as a thread-safe set.
        private static readonly ConcurrentDictionary<string, byte> sessions =
            new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        // Serialises the create path of GetSession. Covers two hazards:
        // (1) two concurrent first-callers for the same key both building a
        // runspace and one's HttpRuntime.Cache.Add losing - the loser's
        // runspace would leak; (2) the unguarded static InitialSessionState
        // backing field in ScriptSession.SpeInitialSessionState, which mutates
        // Commands/Types/Formats collections and is not safe for concurrent
        // first-time init. Reads do not take this lock; only the create path.
        private static readonly object createLock = new object();

        public static ScriptSession GetSession(string persistentId, string defaultId)
        {
            var sessionId = persistentId.IfNullOrEmpty(defaultId);
            return GetSession(sessionId);
        }

        public static ScriptSession NewSession(string applianceType, bool personalizedSettings)
        {
            return GetSession(string.Empty, applianceType, personalizedSettings);
        }

        public static ScriptSession GetSession(string persistentId)
        {
            return GetSession(persistentId, ApplicationNames.Default, false);
        }

        public static bool SessionExists(string persistentId)
        {
            var sessionKey = GetSessionKey(persistentId);
            return sessions.ContainsKey(sessionKey) && HttpRuntime.Cache[sessionKey] != null;
        }

        public static ScriptSession GetSessionIfExists(string persistentId)
        {
            return SessionExists(persistentId) ? GetSession(persistentId) : null;
        }

        // Direct lookup by the full cache key (scriptSession|httpSessionId|persistentId).
        // Unlike GetSession / GetSessionIfExists, this does not reconstruct the key
        // from the caller's current HttpContext, so it can resolve a session
        // belonging to a different browser session of the same user - which is what
        // the ISE session gallery needs to kill a peer session with the same
        // persistent id.
        public static ScriptSession GetSessionByKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            return sessions.ContainsKey(key) ? HttpRuntime.Cache[key] as ScriptSession : null;
        }

        public static bool SessionExistsForAnyUserSession(string persistentId)
        {
            // Cache keys are shaped "$scriptSession$|<httpSessionId>|<persistentId>"
            // (see GetSessionKey), so finding a session for a given persistent id
            // across HttpContext sessions is a pure suffix match on "|<persistentId>".
            var suffix = "|" + persistentId;
            foreach (var kv in sessions)
            {
                if (kv.Key.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) &&
                    HttpRuntime.Cache[kv.Key] != null)
                {
                    return true;
                }
            }
            return false;
        }

        public static IEnumerable<ScriptSession> GetMatchingSessionsForAnyUserSession(string persistentId)
        {
            // Returns a stable List snapshot rather than a deferred LINQ chain
            // so callers (FirstOrDefault / ToList / ForEach) do not enumerate
            // the live ConcurrentDictionary against ongoing writers from
            // unrelated requests.
            var suffix = "|" + persistentId;
            var results = new List<ScriptSession>();
            foreach (var kv in sessions)
            {
                if (!kv.Key.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) continue;
                if (HttpRuntime.Cache[kv.Key] is ScriptSession session)
                {
                    results.Add(session);
                }
            }
            return results;
        }

        public static void RemoveSession(string key)
        {
            // Remove from the index first. HttpRuntime.Cache.Remove fires the
            // CacheItemRemoved callback which re-enters this method, so the
            // TryRemove acts as the idempotency guard against double-dispose.
            sessions.TryRemove(key, out _);
            if (HttpRuntime.Cache[key] == null) return;

            var session = HttpRuntime.Cache.Remove(key) as ScriptSession;
            session?.Dispose();
            PowerShellLog.Debug($"[Session] action=disposed session={key}");
        }

        public static void RemoveSession(ScriptSession session)
        {
            RemoveSession(session.Key);
        }

        public static ScriptSession GetSession(string persistentId, string applianceType, bool personalizedSettings)
        {
            // sessions with no persistent ID, are just created new every time
            var autoDispose = string.IsNullOrEmpty(persistentId);
            if (autoDispose)
            {
                persistentId = Guid.NewGuid().ToString();
            }

            var sessionKey = GetSessionKey(persistentId);

            // Fast path: already-cached session reads lock-free.
            if (sessions.ContainsKey(sessionKey))
            {
                if (HttpRuntime.Cache[sessionKey] is ScriptSession existing)
                {
                    return existing;
                }
            }

            // Slow path: serialise creation behind createLock. See the
            // createLock declaration for the hazards it covers. The fast-path
            // early return above keeps this lock off the hot read path.
            lock (createLock)
            {
                if (sessions.ContainsKey(sessionKey) &&
                    HttpRuntime.Cache[sessionKey] is ScriptSession cached)
                {
                    return cached;
                }

                var session = new ScriptSession(applianceType, personalizedSettings)
                {
                    ID = persistentId,
                };

                PowerShellLog.Debug($"[Session] action=created session={sessionKey}");

                if (autoDispose)
                {
                    // this only should be set if new session has been created - do not change!
                    session.AutoDispose = true;
                }
                var expiration = Sitecore.Configuration.Settings.GetIntSetting(expirationSetting, 30);
                var preventSessionCleanup = Sitecore.Configuration.Settings.GetBoolSetting(preventSessionCleanupSetting, false);

                HttpRuntime.Cache.Add(sessionKey, session, null, Cache.NoAbsoluteExpiration,
                    new TimeSpan(0, expiration, 0), preventSessionCleanup ? CacheItemPriority.NotRemovable : CacheItemPriority.Normal, CacheItemRemoved);

                sessions.TryAdd(sessionKey, 0);
                session.ID = persistentId;
                session.Key = sessionKey;
                session.Initialize();
                return session;
            }
        }

        private static void CacheItemRemoved(string sessionKey, object value, CacheItemRemovedReason reason)
        {
            // Dispose via the value parameter. A prior version routed through
            // RemoveSession, which early-returns when Cache[key] is null - and
            // by the time this callback fires for a TTL eviction the cache
            // entry is already gone, so the session was never disposed. The
            // runspace + host + impersonation context leaked until GC.
            //
            // Runs on a thread-pool thread without HttpContext.Current. The
            // disposal chain (Runspace.Dispose, ScriptSessionManager.RemoveSession,
            // PowerShellLog) does not depend on HttpContext, so this is safe.
            // Caveat: if the session has a running pipeline at eviction time,
            // Runspace.Dispose blocks until it stops; expected because TTL is
            // 30min and scripts are usually short.
            sessions.TryRemove(sessionKey, out _);
            (value as ScriptSession)?.Dispose();
            PowerShellLog.Debug($"[Session] action=disposed session={sessionKey} reason={reason}");
        }

        public static void Clear()
        {
            // Take createLock so a concurrent GetSession on the slow path
            // cannot observe (index has key && Cache[key] == null) between
            // our Cache.Remove and sessions.TryRemove, build a replacement
            // session into the cache, and have its index entry stripped on
            // the next iteration. Without the lock, that race orphans a
            // freshly-created session that is invisible to GetAll /
            // SessionExists / GetMatchingSessionsForAnyUserSession.
            lock (createLock)
            {
                // Snapshot the keys with ToArray so the foreach iterates a
                // stable copy while we mutate the underlying dictionary.
                foreach (var key in sessions.Keys.ToArray())
                {
                    sessions.TryRemove(key, out _);
                    HttpRuntime.Cache.Remove(key);
                }
            }
        }

        public static List<ScriptSession> GetAll()
        {
            var results = new List<ScriptSession>();
            foreach (var kv in sessions)
            {
                if (HttpRuntime.Cache[kv.Key] is ScriptSession session)
                {
                    results.Add(session);
                }
            }
            return results;
        }

        private static string GetSessionKey(string persistentId)
        {
            if (persistentId != null && persistentId.StartsWith(sessionIdPrefix))
            {
                return persistentId;
            }
            var key = new StringBuilder();
            key.Append(sessionIdPrefix);
            key.Append("|");

            if (HttpContext.Current != null && HttpContext.Current.Session != null)
            {
                key.Append(HttpContext.Current.Session.SessionID);
                key.Append("|");
            }
            key.Append(persistentId);
            return key.ToString();
        }

    }
}
