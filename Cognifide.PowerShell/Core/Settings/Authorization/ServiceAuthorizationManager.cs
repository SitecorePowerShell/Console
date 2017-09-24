using System;
using System.Collections.Generic;
using System.Web;
using System.Xml;
using Cognifide.PowerShell.Core.Diagnostics;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore;
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

        public static bool IsUserAuthorized(string serviceName, string userName = null)
        {
            if (!WebServiceSettings.IsEnabled(serviceName))
            {
                return false;
            }
            var authEntries = GetServiceAuthorizationInfo(serviceName);
            var cacheKey = GetAuthorizationCacheKey(serviceName, userName);
            if (string.IsNullOrEmpty(userName))
            {
                return false;
            }

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

        public static bool TerminateUnauthorizedRequest(string serviceName, string userName = null)
        {
            if(!IsUserAuthorized(serviceName,userName))
            {
                if (HttpContext.Current != null && Context.Site != null)
                {
                    HttpContext.Current.Response.Redirect(Context.Site.LoginPage, true);
                }
                return true;
            }
            return false;
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

        private static String GetAuthorizationCacheKey(String serviceName, String userName)
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
                    if (node.Name.Is("#comment"))
                    {
                        continue;
                    }
                    if (AuthorizationEntry.TryParse(node, out entry))
                    {
                        authEntryList.Add(entry);
                    }
                    else
                    {
                        PowerShellLog.Error($"Invalid permission entry for service '{serviceName}'");
                    }
                }
            }
            return authEntryList;
        }

    }
}