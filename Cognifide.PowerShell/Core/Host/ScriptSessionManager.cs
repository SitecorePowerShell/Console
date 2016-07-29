using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Web;
using System.Web.Caching;
using Cognifide.PowerShell.Core.Diagnostics;
using Cognifide.PowerShell.Core.Settings;
using Sitecore.ContentSearch.Linq;

namespace Cognifide.PowerShell.Core.Host
{
    public static class ScriptSessionManager
    {
        private const string sessionIdPrefix = "$scriptSession$";
        private const string expirationSetting = "Cognifide.PowerShell.PersistentSessionExpirationMinutes";
        private static readonly HashSet<string> sessions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static ScriptSession GetSession(string persistentId, string defaultId)
        {
            var sessionId = string.IsNullOrEmpty(persistentId) ? defaultId : persistentId;
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
            lock (sessions)
            {
                return sessions.Contains(sessionKey) && HttpRuntime.Cache[sessionKey] != null;
            }
        }

        public static bool SessionExistsForAnyUserSession(string persistentId)
        {
            var wildcard = new WildcardPattern("*|"+persistentId, WildcardOptions.IgnoreCase);
            lock (sessions)
            {
                return sessions.Any(id => wildcard.IsMatch(id) && HttpRuntime.Cache[id] != null);
            }
        }

        public static IEnumerable<ScriptSession> GetMatchingSessionsForAnyUserSession(string persistentId)
        {
            var wildcard = new WildcardPattern("*|" + persistentId, WildcardOptions.IgnoreCase);
            lock (sessions)
            {
                return
                    sessions.Where(id => wildcard.IsMatch(id) && HttpRuntime.Cache[id] != null)
                        .Select(id => HttpRuntime.Cache[id] as ScriptSession);
            }
        }

        public static void RemoveSession(string key)
        {
            lock (sessions)
            {
                if (sessions.Contains(key))
                {
                    sessions.Remove(key);
                }
                if (HttpRuntime.Cache[key] == null) return;

                var session = HttpRuntime.Cache.Remove(key) as ScriptSession;
                if (session != null)
                {
                    session.Dispose();
                }
                PowerShellLog.Debug($"Script Session '{key}' disposed.");
            }
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
            lock (sessions)
            {
                if (SessionExists(persistentId))
                {
                    return HttpRuntime.Cache[sessionKey] as ScriptSession;
                }

                var session = new ScriptSession(applianceType, personalizedSettings)
                {
                    ID = persistentId,
                };

                PowerShellLog.Debug($"New Script Session with key '{sessionKey}' created.");

                if (autoDispose)
                {
                    // this only should be set if new session has been created - do not change!
                    session.AutoDispose = true;
                }
                var expiration = Sitecore.Configuration.Settings.GetIntSetting(expirationSetting, 30);
                HttpRuntime.Cache.Add(sessionKey, session, null, Cache.NoAbsoluteExpiration,
                    new TimeSpan(0, expiration, 0), CacheItemPriority.Normal, CacheItemRemoved);
                sessions.Add(sessionKey);
                session.ID = persistentId;
                session.Key = sessionKey;
                session.Initialize();
                return session;
            }
        }

        private static void CacheItemRemoved(string sessionKey, Object value, CacheItemRemovedReason reason)
        {
            RemoveSession(sessionKey);
        }

        public static void Clear()
        {
            lock (sessions)
            {
                foreach (var key in sessions)
                {
                    var sessionKey = GetSessionKey(key);
                    var session = HttpRuntime.Cache.Remove(sessionKey) as ScriptSession;
                    if (session != null)
                    {
                        session.Dispose();
                    }
                }
                sessions.Clear();
            }
        }

        public static List<ScriptSession> GetAll()
        {
            lock (sessions)
            {
                return sessions.Select(sessionKey => HttpRuntime.Cache[sessionKey] as ScriptSession)
                    .Where(s => s != null)
                    .ToList();
            }
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