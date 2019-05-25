using System;
using System.Collections.Generic;
using System.Web;
using System.Xml;
using Sitecore.Configuration;
using Spe.Core.Diagnostics;

namespace Spe.Core.Settings.Authorization
{
    public static class SessionElevationManager
    {
        private const string SessionCacheToken = "SPE_Session_Elevation_{0}";
        private static readonly Dictionary<string, TokenDefinition> Tokens;

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

                if (TimeSpan.TryParse(xmlDefinition.Attributes["expiration"].Value, out var expiration))
                {
                    token.Expiration = expiration;
                }

                token.Action = Enum.TryParse(xmlDefinition.Attributes["elevationAction"].Value, out TokenDefinition.ElevationAction action)
                    ? action
                    : TokenDefinition.ElevationAction.Block;
                tokenDefinitions.Add(token.Name, token);
            }
            Tokens = new Dictionary<string, TokenDefinition>();
            var xmlApps = Factory.GetConfigNodes("powershell/userAccountControl/gates/gate");
            foreach (XmlElement xmlApp in xmlApps)
            {
                var name = xmlApp.Attributes["name"].Value;
                if (Tokens.ContainsKey(name))
                {
                    throw new ArgumentException($"A duplicate gate was detected in the configuration. The gate '{name}' already exists.");
                }

                Tokens.Add(name, tokenDefinitions[xmlApp.Attributes["token"].Value]);
            }
        }

        internal static TokenDefinition GetToken(string appName)
        {
            return Tokens.ContainsKey(appName) ? Tokens[appName] : Tokens["Default"];
        }

        public static bool IsSessionTokenElevated(string appName)
        {
            var token = GetToken(appName);
            switch (token.Action)
            {
                case TokenDefinition.ElevationAction.Allow:
                    return true;
                case TokenDefinition.ElevationAction.Password:
                    var cachedSession = HttpContext.Current?.Session[string.Format(SessionCacheToken, token.Name)];
                    if (cachedSession == null || ((DateTime) cachedSession >= DateTime.Now)) return cachedSession != null;
                    PowerShellLog.Warn($"Session state elevation expired for '{appName}' for user: {Sitecore.Context.User?.Name}");
                    HttpContext.Current.Session.Remove(string.Format(SessionCacheToken, token.Name));
                    return false;
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