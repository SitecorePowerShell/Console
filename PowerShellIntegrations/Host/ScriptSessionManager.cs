using System.Web;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;

namespace Cognifide.PowerShell.PowerShellIntegrations.Host
{
    public static class ScriptSessionManager
    {
        //private static readonly HashSet<string> sessions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private const string sessionIdPrefix = "$scriptSession$";

        public static ScriptSession GetSession(string persistentId)
        {
            return GetSession(persistentId, ApplicationNames.Default, false);
        }

        public static bool SessionExists(string persistentId)
        {
            return HttpContext.Current.Session[sessionIdPrefix + persistentId] != null;
        }

        public static void RemoveSession(string persistentId)
        {
            HttpContext.Current.Session.Remove(sessionIdPrefix + persistentId);
        }

        public static ScriptSession GetSession(string persistentId, string applicanceType, bool personalizedSettings)
        {
            // sessions with no persistent ID, are just created new every time
            if (string.IsNullOrEmpty(persistentId))
            {
                return new ScriptSession(applicanceType, personalizedSettings);
            }

            lock (HttpContext.Current.Session)
            {
                if (SessionExists(persistentId))
                {
                    return HttpContext.Current.Session[sessionIdPrefix + persistentId] as ScriptSession;
                }

                var session = new ScriptSession(applicanceType, personalizedSettings);
                session.ID = persistentId;
                HttpContext.Current.Session[sessionIdPrefix + persistentId] = session;
                session.ID = persistentId;
                session.Initialize();
                session.ExecuteScriptPart(session.Settings.Prescript);
                return session;
            }
        }

        public static void Clear()
        {
            //copy dictionary to release lock quickly
            lock (HttpContext.Current.Session)
            {
                foreach (string key in HttpContext.Current.Session.Keys)
                {
                    if (key.StartsWith(sessionIdPrefix))
                    {
                        var session = HttpContext.Current.Session[key] as ScriptSession;
                        if (session != null)
                        {
                            session.Dispose();
                        }
                    }
                }
            }
        }
    }
}