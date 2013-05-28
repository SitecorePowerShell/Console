using System.Collections.Generic;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;

namespace Cognifide.PowerShell.PowerShellIntegrations.Host
{
    public static class ScriptSessionManager
    {
        private static readonly Dictionary<string, ScriptSession> cache = new Dictionary<string, ScriptSession>();

        public static ScriptSession GetSession(string persistentId)
        {
            return GetSession(persistentId, ApplicationNames.Default, false);
        }

        public static ScriptSession GetSession(string persistentId, string applicanceType, bool personalizedSettings)
        {
            // sessions with no persistent ID, are just created new every time
            if (string.IsNullOrEmpty(persistentId))
            {
                return new ScriptSession(applicanceType, personalizedSettings);
            }

            lock (cache)
            {
                if (cache.ContainsKey(persistentId))
                {
                    return cache[persistentId];
                }
                
                var session = new ScriptSession(applicanceType, personalizedSettings);
                cache.Add(persistentId, session);
                return session;
            }
        }

        public static void Clear()
        {
            Dictionary<string, ScriptSession> tempCache;

            //copy dictionary to release lock quickly
            lock (cache)
            {
                tempCache = new Dictionary<string, ScriptSession>(cache);
                cache.Clear();
            }

            foreach (var session in tempCache)
            {
                session.Value.Dispose();
            }
        }
    }
}