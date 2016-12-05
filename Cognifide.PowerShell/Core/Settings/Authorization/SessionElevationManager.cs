using System;
using System.Collections.Generic;
using System.Web;
using System.Xml;
using Cognifide.PowerShell.Core.Diagnostics;
using Sitecore.Configuration;

namespace Cognifide.PowerShell.Core.Settings.Authorization
{
    public static class SessionElevationManager
    {
        private const string SessionCacheToken = "SPE_Session_Elevation_{0}";
        private static Dictionary<string, TokenDefinition> tokens;

        public const string SaveAction = "Save";
        public const string ExecuteAction = "Execute";

        static SessionElevationManager()
        {
            var tokenDefinitions = new Dictionary<string, TokenDefinition>();
            var xmlDefinitions = Factory.GetConfigNodes("powershell/userAccountControl/tokens/token");
            foreach (XmlElement xmlDefinition in xmlDefinitions)
            {
                var token = new TokenDefinition()
                {
                    Name = xmlDefinition.Attributes["name"].Value
                };

                if (tokenDefinitions.ContainsKey(token.Name))
                {
                    throw new ArgumentException($"A duplicate token was detected in the configuration. The token '{token.Name}' already exists.");
                }

                TimeSpan expiration;
                if (TimeSpan.TryParse(xmlDefinition.Attributes["expiration"].Value, out expiration))
                {
                    token.Expiration = expiration;
                }

                TokenDefinition.ElevationAction action;
                token.Action = Enum.TryParse(xmlDefinition.Attributes["elevationAction"].Value, out action)
                    ? action
                    : TokenDefinition.ElevationAction.Block;
                tokenDefinitions.Add(token.Name, token);
            }
            tokens = new Dictionary<string, TokenDefinition>();
            var xmlApps = Factory.GetConfigNodes("powershell/userAccountControl/gates/gate");
            foreach (XmlElement xmlApp in xmlApps)
            {
                var name = xmlApp.Attributes["name"].Value;
                if (tokens.ContainsKey(name))
                {
                    throw new ArgumentException($"A duplicate gate was detected in the configuration. The gate '{name}' already exists.");
                }

                tokens.Add(name, tokenDefinitions[xmlApp.Attributes["token"].Value]);
            }
        }

        internal static TokenDefinition GetToken(string appName)
        {
            return tokens.ContainsKey(appName) ? tokens[appName] : tokens["Default"];
        }

        public static bool IsSessionTokenElevated(string appName)
        {
            var token = GetToken(appName);
            switch (token.Action)
            {
                case TokenDefinition.ElevationAction.Allow:
                    return true;
                case TokenDefinition.ElevationAction.Password:
                    var sessionvar = HttpContext.Current?.Session[string.Format(SessionCacheToken, token.Name)];
                    if (sessionvar != null && ((DateTime)sessionvar < DateTime.Now))
                    {
                        PowerShellLog.Warn($"Session state elevation expired for '{appName}' for user: {Sitecore.Context.User?.Name}");
                        HttpContext.Current.Session.Remove(string.Format(SessionCacheToken, token.Name));
                        sessionvar = null;
                    }
                    return sessionvar != null;
                default:
                    return false;
            }
        }

        public static void ElevateSessionToken(string appName)
        {
            var token = GetToken(appName);
            PowerShellLog.Warn($"Session state elevated for '{appName}' by user: {Sitecore.Context.User?.Name}");
            HttpContext.Current.Session[string.Format(SessionCacheToken, token?.Name)] = DateTime.Now + token.Expiration;
        }

        public static void DropSessionTokenElevation(string appName)
        {
            var token = GetToken(appName);
            HttpContext.Current.Session.Remove(string.Format(SessionCacheToken, token?.Name));
            PowerShellLog.Warn($"Session state elevation dropped for '{appName}' by user: {Sitecore.Context.User?.Name}");
        }

        internal class TokenDefinition
        {
            public string Name { get; set; }
            public TimeSpan Expiration { get; set; }
            public ElevationAction Action { get; set; }

            internal enum ElevationAction
            {
                Block,
                Password,
                Allow,
            }
        }
    }
}