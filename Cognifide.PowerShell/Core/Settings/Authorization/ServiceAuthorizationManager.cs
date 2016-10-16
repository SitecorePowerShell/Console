using System;
using System.Collections.Generic;
using System.Xml;
using Cognifide.PowerShell.Core.Diagnostics;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Configuration;
using Sitecore.Security.AccessControl;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Core.Settings.Authorization
{
    public static class ServiceAuthorizationManager
    {

        private static Dictionary<string, List<AuthorizationEntry>> authorizationEntries =
            new Dictionary<string, List<AuthorizationEntry>>();

        private static Dictionary<string, AuthCacheEntry> authorizationCache = new Dictionary<string, AuthCacheEntry>();

        public static bool IsUserAuthorized(string serviceName, string userName, bool defaultValue)
        {
            var authEntries = GetServiceAuthorizationInfo(serviceName);
            var cacheKey = GetAuthorizationCacheKey(serviceName, userName);

            if (ExistsInCache(cacheKey))
            {
                lock (authorizationCache)
                {
                    return authorizationCache[cacheKey].Authorized;
                }
            }

            bool? allowedByRole = null;
            bool? allowedByName = null;

            var user = User.FromName(userName, false);

            foreach (var authEntry in authEntries)
            {
                switch (authEntry.IdentityType)
                {
                    case AccountType.Role:
                        Role role = authEntry.Identity;
                        if (!allowedByRole.HasValue || allowedByRole.Value)
                            // if not denied by previous rules - keep checking
                        {
                            if ((role != null && user.IsInRole(role)) ||
                                // check for special role based on user having administrator privileges
                                ("sitecore\\IsAdministrator".Equals(authEntry.Identity.Name,
                                     StringComparison.InvariantCultureIgnoreCase) && user.IsAdministrator))
                            {
                                allowedByRole = authEntry.AccessPermission == AccessPermission.Allow;
                            }
                        }
                        break;
                    case AccountType.User:
                        if (!allowedByName.HasValue || allowedByName.Value)
                            // if not denied by previous rules - keep checking
                        {
                            if (authEntry.WildcardMatch(userName))
                            {
                                allowedByName = authEntry.AccessPermission == AccessPermission.Allow;
                                if (!allowedByName.Value)
                                {
                                    break;
                                }
                            }
                        }
                        break;
                }
            }

            bool allowed = false;
            if (allowedByName.HasValue)
            {
                allowed = allowedByName.Value;
            }
            else if (allowedByRole.HasValue)
            {
                allowed = allowedByRole.Value;
            }

            lock (authorizationCache)
            {
                authorizationCache[cacheKey] = new AuthCacheEntry()
                {
                    Authorized = allowed,
                    ExpirationDate = DateTime.Now.AddSeconds(WebServiceSettings.AuthorizationCacheExpirationSecs)
                };
            }

            return allowed;
        }

        private static bool ExistsInCache(string cacheKey)
        {
            // cache health check
            lock (authorizationCache)
            {
                if (authorizationCache.Keys.Count > 1000)
                {
                    authorizationCache.Clear();
                }
                return authorizationCache.ContainsKey(cacheKey) &&
                       authorizationCache[cacheKey].ExpirationDate > DateTime.Now;
            }
        }

        private static System.String GetAuthorizationCacheKey(System.String serviceName, System.String userName)
        {
            return userName + "@" + serviceName;
        }

        private static List<AuthorizationEntry> GetServiceAuthorizationInfo(string serviceName)
        {
            if (authorizationEntries.ContainsKey(serviceName))
            {
                return authorizationEntries[serviceName];
            }

            var authEntryList = new List<AuthorizationEntry>();
            authorizationEntries.Add(serviceName, authEntryList);

            var servicesNode =
                Factory.GetConfigNode($"powershell/services/{serviceName}/authorization");

            if (servicesNode != null)
            {
                foreach (XmlNode node in servicesNode.ChildNodes)
                {
                    AuthorizationEntry entry;
                    if (AuthorizationEntry.TryParse(node, out entry))
                    {
                        authEntryList.Add(entry);
                    }
                    else
                    {
                        PowerShellLog.Error($"Invalid permission entry for service {serviceName}");
                    }
                }
            }
            return authEntryList;
        }

    }
}