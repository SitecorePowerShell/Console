using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.IO;
using Spe.Core.Extensions;

namespace Spe.Core.Settings.Authorization
{
    public static class WebServiceSettings
    {
        internal class CorsSettings
        {
            public HashSet<string> AllowedOrigins { get; set; }
            public bool AllowAnyOrigin { get; set; }
            public bool AllowCredentials { get; set; }
            public int MaxAge { get; set; }
        }

        private class ServiceState
        {
            public bool Enabled { get; set; }
            public bool RequireSecureConnection { get; set; }
            public CorsSettings Cors { get; set; }
        }

        private static readonly Dictionary<string, ServiceState> services = new Dictionary<string, ServiceState>();

        // Spe.Remoting.UseForwardedHeaders governs whether X-Forwarded-Proto and
        // X-Forwarded-For are trusted. Default true preserves pre-9.0 behavior;
        // hardened mode (false) is opt-in for direct-IIS deployments. Cached
        // once at static-ctor time, matching the rest of this class - flipping
        // the setting requires an app-domain recycle.
        public static bool UseForwardedHeaders { get; private set; }

        // Pipe-delimited absolute paths that fileDownload/fileUpload may serve
        // when the caller passes an unrecognized origin (e.g. "custom").
        // Default empty: any non-alias origin paired with an absolute path is
        // rejected. Each entry is canonicalized + suffixed with the platform
        // separator so prefix matching cannot leak across siblings.
        public static string[] AllowedFileRoots { get; private set; }

        // When true, remoting error responses expose full exception details
        // (scriptStackTrace, exception type/message, blocked-command + policy
        // names, IOException text). When false (default) the response carries
        // only a correlation id (the per-request rid), the PowerShell
        // ErrorCategory, and the FullyQualifiedErrorId. The full details stay
        // in the audit log; operators correlate by rid.
        public static bool DetailedErrors { get; private set; }

        public const string ServiceRestfulv1 = "restfulv1";
        public const string ServiceRestfulv2 = "restfulv2";
        public const string ServiceRemoting = "remoting";
        public const string ServiceClient = "client";
        public const string ServiceExecution = "execution";
        public const string ServiceFileDownload = "fileDownload";
        public const string ServiceFileUpload = "fileUpload";
        public const string ServiceMediaDownload = "mediaDownload";
        public const string ServiceMediaUpload = "mediaUpload";
        public const string ServiceHandleDownload = "handleDownload";

        static WebServiceSettings()
        {
            var servicesNodes = Factory.GetConfigNode("powershell/services").ChildNodes;
            foreach (XmlElement xmlDefinition in servicesNodes)
            {
                if (services.ContainsKey(xmlDefinition.Name))
                {
                    throw new ArgumentException($"A duplicate service name was detected in the configuration. The service '{xmlDefinition.Name}' already exists.");
                }

                var service = new ServiceState()
                {
                    Enabled = xmlDefinition.Attributes["enabled"]?.Value?.Is("true") == true,
                    RequireSecureConnection = xmlDefinition.Attributes["requireSecureConnection"]?.Value?.Is("true") == true
                };

                var corsNode = xmlDefinition.SelectSingleNode("cors") as XmlElement;
                if (corsNode != null)
                {
                    var origins = corsNode.GetAttribute("allowedOrigins");
                    if (!string.IsNullOrEmpty(origins))
                    {
                        var allowCredentials = corsNode.GetAttribute("allowCredentials").Is("true");
                        var allowAnyOrigin = origins == "*";

                        if (allowCredentials && allowAnyOrigin)
                        {
                            Sitecore.Diagnostics.Log.Warn(
                                "[SPE] CORS misconfiguration: allowCredentials cannot be used with wildcard allowedOrigins. Credentials will not be allowed.", typeof(WebServiceSettings));
                            allowCredentials = false;
                        }

                        service.Cors = new CorsSettings
                        {
                            AllowAnyOrigin = allowAnyOrigin,
                            AllowedOrigins = allowAnyOrigin
                                ? null
                                : new HashSet<string>(origins.Split('|'), StringComparer.OrdinalIgnoreCase),
                            AllowCredentials = allowCredentials,
                            MaxAge = int.TryParse(corsNode.GetAttribute("maxAge"), out var m) ? m : 3600
                        };
                    }
                }

                services.Add(xmlDefinition.Name, service);
            }

            CommandWaitMillis = Sitecore.Configuration.Settings.GetIntSetting("Spe.CommandWaitMillis", 25);
            InitialPollMillis = Sitecore.Configuration.Settings.GetIntSetting("Spe.InitialPollMillis", 100);
            MaxmimumPollMillis = Sitecore.Configuration.Settings.GetIntSetting("Spe.MaxmimumPollMillis", 2500);
            AuthorizationCacheExpirationSecs = Sitecore.Configuration.Settings.GetIntSetting("Spe.AuthorizationCacheExpirationSecs", 10);
            var settingStr = Sitecore.Configuration.Settings.GetSetting("Spe.SerializationSizeBuffer", "5KB");
            var sizeLong = StringUtil.ParseSizeString(settingStr);
            SerializationSizeBuffer = (int)(sizeLong < int.MaxValue ? sizeLong : int.MaxValue);

            UseForwardedHeaders = Sitecore.Configuration.Settings.GetBoolSetting("Spe.Remoting.UseForwardedHeaders", true);

            AllowedFileRoots = ParseAllowedFileRoots(
                Sitecore.Configuration.Settings.GetSetting("Spe.Remoting.AllowedFileRoots", string.Empty));

            DetailedErrors = Sitecore.Configuration.Settings.GetBoolSetting("Spe.Remoting.DetailedErrors", false);

            // Operators on direct-IIS (no reverse proxy) should set
            // Spe.Remoting.UseForwardedHeaders=false so spoofed headers cannot bypass
            // requireSecureConnection or pollute audit logs. Warn once if the
            // setting is at its default and any service requires TLS.
            var explicitSetting = Sitecore.Configuration.Settings.GetSetting("Spe.Remoting.UseForwardedHeaders", null);
            if (string.IsNullOrEmpty(explicitSetting) && UseForwardedHeaders)
            {
                foreach (var entry in services)
                {
                    if (entry.Value.RequireSecureConnection)
                    {
                        Sitecore.Diagnostics.Log.Warn(
                            $"[SPE] Spe.Remoting.UseForwardedHeaders is at its default (true) and service '{entry.Key}' has requireSecureConnection=true. " +
                            "If this server has no reverse proxy in front of it, set Spe.Remoting.UseForwardedHeaders=false to reject spoofed X-Forwarded-Proto and X-Forwarded-For headers. " +
                            "The default flips to false in 10.0.",
                            typeof(WebServiceSettings));
                        break;
                    }
                }
            }
        }

        public static int CommandWaitMillis { get; private set; }
        public static int InitialPollMillis { get; private set; }
        public static int MaxmimumPollMillis { get; private set; }
        public static int SerializationSizeBuffer { get; private set; }
        public static int AuthorizationCacheExpirationSecs { get; set; }

        internal static CorsSettings GetCorsSettings(string serviceName)
        {
            if (services.ContainsKey(serviceName))
            {
                return services[serviceName].Cors;
            }
            return null;
        }

        public static bool IsEnabled(string serviceName)
        {
            if (!services.ContainsKey(serviceName))
            {
                return false;
            }
            var service = services[serviceName];
            return service.Enabled && CheckSecureConnectionRequirement(service);
        }

        // True for paths with an explicit Windows drive letter ("C:\foo", "C:foo")
        // or UNC root ("\\server\share", "//server/share"). False for Sitecore-
        // relative paths like "/App_Data" - those are rooted in .NET's eyes
        // (Path.IsPathRooted returns true) but should be resolved through
        // FileUtil.MapPath against the application root, not Path.GetFullPath
        // against the current drive.
        internal static bool HasExplicitDriveOrUnc(string path)
        {
            if (string.IsNullOrEmpty(path) || path.Length < 2) return false;
            if (path[1] == ':') return true;
            if ((path[0] == '\\' || path[0] == '/') && path[0] == path[1]) return true;
            return false;
        }

        private static string[] ParseAllowedFileRoots(string rawSetting)
        {
            if (string.IsNullOrWhiteSpace(rawSetting))
            {
                return Array.Empty<string>();
            }

            var roots = new List<string>();
            foreach (var raw in rawSetting.Split('|'))
            {
                var trimmed = raw.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                string resolved;
                try
                {
                    resolved = HasExplicitDriveOrUnc(trimmed)
                        ? Path.GetFullPath(trimmed)
                        : Path.GetFullPath(FileUtil.MapPath(trimmed));
                }
                catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
                {
                    Sitecore.Diagnostics.Log.Warn(
                        $"[SPE] Spe.Remoting.AllowedFileRoots ignored invalid entry '{trimmed}': {ex.Message}",
                        typeof(WebServiceSettings));
                    continue;
                }

                roots.Add(resolved.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar);
            }

            return roots.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        private static bool CheckSecureConnectionRequirement(ServiceState stateOfService)
        {
            var request = HttpContext.Current?.Request;
            if (request == null) return true;

            return ForwardedHeaderHelper.ShouldAcceptForwardedProto(
                stateOfService.RequireSecureConnection,
                request.IsSecureConnection,
                request.Headers["X-Forwarded-Proto"],
                UseForwardedHeaders);
        }

    }
}
