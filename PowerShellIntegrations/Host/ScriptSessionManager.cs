using System;
using System.Collections.Generic;
using System.Linq;
//using System.Web;
using System.Web;
using System.Web.Caching;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
using Sitecore.Shell.Framework.Commands.TemplateBuilder;

namespace Cognifide.PowerShell.PowerShellIntegrations.Host
{
    public static class ScriptSessionManager
    {
        private static readonly HashSet<string> sessions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private const string sessionIdPrefix = "$scriptSession$";

        public static ScriptSession GetSession(string persistentId)
        {
            return GetSession(persistentId, ApplicationNames.Default, false);
        }

        public static bool SessionExists(string persistentId)
        {            
            return sessions.Contains(persistentId) && HttpRuntime.Cache[sessionIdPrefix + persistentId] != null;
        }

        public static void RemoveSession(string persistentId)
        {
            lock (sessions)
            {
                sessions.Remove(persistentId);
                ScriptSession session = HttpRuntime.Cache.Remove(sessionIdPrefix + persistentId) as ScriptSession;
                if (session != null)
                {
                    session.Dispose();
                }
            }
        }

        public static void RemoveSession(ScriptSession session)
        {
            RemoveSession(session.ID);
        }

        public static ScriptSession GetSession(string persistentId, string applicanceType, bool personalizedSettings)
        {
            // sessions with no persistent ID, are just created new every time
            bool autoDispose = string.IsNullOrEmpty(persistentId);
            if(autoDispose)
            {
                persistentId = Guid.NewGuid().ToString();
                //return new ScriptSession(applicanceType, personalizedSettings);
            }

            lock (sessions)
            {
                if (SessionExists(persistentId))
                {
                    return HttpRuntime.Cache[sessionIdPrefix + persistentId] as ScriptSession;
                }

                var session = new ScriptSession(applicanceType, personalizedSettings);
                session.ID = persistentId;
                session.AutoDispose = autoDispose;
                HttpRuntime.Cache[sessionIdPrefix + persistentId] = session;
                sessions.Add(persistentId);
                session.ID = persistentId;
                session.Initialize();
                session.ExecuteScriptPart(session.Settings.Prescript);
                return session;
            }
        }

        public static void Clear()
        {
            //copy dictionary to release lock quickly
            lock (sessions)
            {
                sessions.Clear();
                foreach (string key in sessions)
                {
                    if (key.StartsWith(sessionIdPrefix))
                    {
                        var session = HttpRuntime.Cache[key] as ScriptSession;
                        if (session != null)
                        {
                            session.Dispose();
                        }
                    }
                }
            }
        }

        public static List<ScriptSession> GetAll()
        {
            //copy dictionary to release lock quickly
            lock (sessions)
            {
                return sessions.Select(key => HttpRuntime.Cache[sessionIdPrefix + key] as ScriptSession)
                    .Where(s => s != null)
                    .ToList();
            }
        }
    }
}