using Sitecore;

namespace Cognifide.PowerShell.PowerShellIntegrations.Settings
{
    public static class WebServiceSettings
    {
        public static int CommandWaitMillis
        {
            get { return Sitecore.Configuration.Settings.GetIntSetting("Cognifide.PowerShell.CommandWaitMillis", 25); }
        }

        public static int InitialPollMillis
        {
            get { return Sitecore.Configuration.Settings.GetIntSetting("Cognifide.PowerShell.InitialPollMillis", 100); }
        }

        public static int MaxmimumPollMillis
        {
            get
            {
                return Sitecore.Configuration.Settings.GetIntSetting("Cognifide.PowerShell.MaxmimumPollMillis", 5000);
            }
        }

        public static int SerializationSizeBuffer
        {
            get
            {
                string settingStr =
                    Sitecore.Configuration.Settings.GetSetting("Cognifide.PowerShell.SerializationSizeBuffer", "5KB");
                long sizeLong = StringUtil.ParseSizeString(settingStr);
                int size = int.MaxValue;
                if (sizeLong < int.MaxValue)
                {
                    size = (int) sizeLong;
                }
                return size;
            }
        }
    }
}