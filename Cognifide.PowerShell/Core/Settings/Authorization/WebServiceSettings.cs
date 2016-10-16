using System;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Core.Settings.Authorization
{
    public static class WebServiceSettings
    {
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
            ServiceEnabledRestfulv1 = IsServiceEnabled(ServiceRestfulv1, false);
            ServiceEnabledRestfulv2 = IsServiceEnabled(ServiceRestfulv2, true);
            ServiceEnabledClient = IsServiceEnabled(ServiceClient, true);
            ServiceEnabledRemoting = IsServiceEnabled(ServiceRemoting, false);
            ServiceEnabledFileDownload = IsServiceEnabled(ServiceFileDownload, false);
            ServiceEnabledFileUpload = IsServiceEnabled(ServiceFileUpload, false);
            ServiceEnabledMediaDownload = IsServiceEnabled(ServiceMediaDownload, false);
            ServiceEnabledMediaUpload = IsServiceEnabled(ServiceMediaUpload, false);
            ServiceEnabledHandleDownload = IsServiceEnabled(ServiceHandleDownload, true);
            CommandWaitMillis = Sitecore.Configuration.Settings.GetIntSetting("Cognifide.PowerShell.CommandWaitMillis", 25);
            InitialPollMillis = Sitecore.Configuration.Settings.GetIntSetting("Cognifide.PowerShell.InitialPollMillis", 100);
            MaxmimumPollMillis = Sitecore.Configuration.Settings.GetIntSetting("Cognifide.PowerShell.MaxmimumPollMillis", 2500);
            AuthorizationCacheExpirationSecs = Sitecore.Configuration.Settings.GetIntSetting("Cognifide.PowerShell.AuthorizationCacheExpirationSecs", 10);
            var settingStr = Sitecore.Configuration.Settings.GetSetting("Cognifide.PowerShell.SerializationSizeBuffer", "5KB");
            var sizeLong = StringUtil.ParseSizeString(settingStr);
            SerializationSizeBuffer = (int) (sizeLong < int.MaxValue ? sizeLong : int.MaxValue);
        }

        public static bool ServiceEnabledFileDownload { get; private set; }
        public static bool ServiceEnabledFileUpload { get; private set; }
        public static bool ServiceEnabledMediaDownload { get; private set; }
        public static bool ServiceEnabledMediaUpload { get; private set; }
        public static bool ServiceEnabledHandleDownload { get; private set; }
        public static bool ServiceEnabledRestfulv1 { get; private set; }
        public static bool ServiceEnabledRestfulv2 { get; private set; }
        public static bool ServiceEnabledRemoting { get; private set; }
        public static bool ServiceEnabledClient { get; private set; }
        public static int CommandWaitMillis { get; private set; }
        public static int InitialPollMillis { get; private set; }
        public static int MaxmimumPollMillis { get; private set; }
        public static int SerializationSizeBuffer { get; private set; }
        public static int AuthorizationCacheExpirationSecs { get; set; }

        private static bool IsServiceEnabled(string serviceName, bool defaultValue)
        {
            var servicesNode = Factory.GetConfigNode($"powershell/services/{serviceName}");
            if (servicesNode == null)
            {
                return defaultValue;
            }
            return string.Equals(servicesNode.Attributes["enabled"].InnerText, "true",
                StringComparison.OrdinalIgnoreCase);
        }
    }
}