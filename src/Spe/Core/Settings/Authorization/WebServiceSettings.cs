using System;
using System.Collections.Generic;
using System.Web;
using System.Xml;
using Sitecore;
using Sitecore.Configuration;
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

        private static bool CheckSecureConnectionRequirement(ServiceState stateOfService)
        {
            if (HttpContext.Current == null || HttpContext.Current.Request == null)
            {
                return true;
            }

            if (!stateOfService.RequireSecureConnection)
            {
                return true;
            }

            if (HttpContext.Current.Request.IsSecureConnection == true)
            {
                return true;
            }
            else
            {
                // Check if request was offloaded on edge web server.
                // Trusts X-Forwarded-Proto header — assumes a trusted reverse proxy strips/sets this header.
                return string.Equals(HttpContext.Current.Request.Headers["X-Forwarded-Proto"], Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
            }
        }

    }
}

