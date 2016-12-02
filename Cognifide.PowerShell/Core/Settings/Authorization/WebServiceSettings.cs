using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Security.Accounts;
using Sitecore.Visual;
using Cognifide.PowerShell.Core.Extensions;

namespace Cognifide.PowerShell.Core.Settings.Authorization
{
    public static class WebServiceSettings
    {

        private class ServiceState
        {
            public bool Enabled { get; set; }
            public bool RequireSecureConnection { get; set; }
        }

        private static Dictionary<string,ServiceState> services = new Dictionary<string,ServiceState>();

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
            var servicesNodes = Factory.GetConfigNode($"powershell/services").ChildNodes;
            foreach (XmlElement xmlDefinition in servicesNodes)
            {
                var service = new ServiceState()
                {
                    Enabled = xmlDefinition.Attributes["enabled"]?.Value?.Is("true") == true,
                    RequireSecureConnection = xmlDefinition.Attributes["requireSecureConnection"]?.Value?.Is("true") == true
                };
                services.Add(xmlDefinition.Name,service);
            }

            CommandWaitMillis = Sitecore.Configuration.Settings.GetIntSetting("Cognifide.PowerShell.CommandWaitMillis", 25);
            InitialPollMillis = Sitecore.Configuration.Settings.GetIntSetting("Cognifide.PowerShell.InitialPollMillis", 100);
            MaxmimumPollMillis = Sitecore.Configuration.Settings.GetIntSetting("Cognifide.PowerShell.MaxmimumPollMillis", 2500);
            AuthorizationCacheExpirationSecs = Sitecore.Configuration.Settings.GetIntSetting("Cognifide.PowerShell.AuthorizationCacheExpirationSecs", 10);
            var settingStr = Sitecore.Configuration.Settings.GetSetting("Cognifide.PowerShell.SerializationSizeBuffer", "5KB");
            var sizeLong = StringUtil.ParseSizeString(settingStr);
            SerializationSizeBuffer = (int) (sizeLong < int.MaxValue ? sizeLong : int.MaxValue);
        }

        public static int CommandWaitMillis { get; private set; }
        public static int InitialPollMillis { get; private set; }
        public static int MaxmimumPollMillis { get; private set; }
        public static int SerializationSizeBuffer { get; private set; }
        public static int AuthorizationCacheExpirationSecs { get; set; }

        public static bool IsEnabled(string serviceName)
        {
            if (!services.Keys.Contains(serviceName))
            {
                return false;
            }
            var service = services[serviceName];
            return service.Enabled &&
                   (HttpContext.Current == null || !service.RequireSecureConnection || HttpContext.Current.Request?.IsSecureConnection == true);
        }
    }
}