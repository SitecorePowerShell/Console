using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Security.Accounts;
using Sitecore.Security.Domains;

namespace Cognifide.PowerShell.PowerShellIntegrations.Settings
{
    public class ApplicationSettings
    {
        private const string SettingsItemPath = "/sitecore/content/Applications/PowerShell/Settings/";
        private const string IseSettingsItemAllUsers = "All Users";
        private const string FolderTemplatePath = "/sitecore/templates/Common/Folder";

        private static readonly Dictionary<string, ApplicationSettings> instances =
            new Dictionary<string, ApplicationSettings>();

        private ApplicationSettings(string applicationName)
        {
            ApplicationName = applicationName;
        }

        protected bool Loaded { get; private set; }

        public string Prescript { get; set; }
        public string LastScript { get; set; }
        public bool SaveLastScript { get; set; }
        public bool UseTypeInfo { get; set; }
        public int HostWidth { get; set; }
        public ConsoleColor ForegroundColor { get; set; }
        public ConsoleColor BackgroundColor { get; set; }
        public string ApplicationName { get; private set; }

        public string AppSettingsPath
        {
            get { return SettingsItemPath + ApplicationName + "/"; }
        }

        public string CurrentUserSettingsPath
        {
            get { return AppSettingsPath + CurrentDomain + "/" + CurrentUserName; }
        }

        public string AllUsersSettingsPath
        {
            get { return AppSettingsPath + IseSettingsItemAllUsers; }
        }

        private static string CurrentUserName
        {
            get { return User.Current.LocalName; }
        }

        private static Domain CurrentDomain
        {
            get { return User.Current.Domain; }
        }

        public static ApplicationSettings GetInstance(string applicationName)
        {
            return GetInstance(applicationName, true);
        }

        public static ApplicationSettings GetInstance(string applicationName, bool personalizedSettings)
        {
            string settingsPath = applicationName +
                                  (personalizedSettings
                                       ? "/" + CurrentDomain + "/" + CurrentUserName
                                       : "/All Users");
            ApplicationSettings instance = null;
            lock (instances)
            {
                if (instances.ContainsKey(settingsPath))
                {
                    instance = instances[settingsPath];
                }
                if (instance == null || !instance.Loaded)
                {
                    instance = new ApplicationSettings(applicationName);
                    instance.Load();
                    instances.Add(settingsPath, instance);
                }
            }
            return instance;
        }

        private Item GetSettingsDto()
        {
            Database coreDb = Factory.GetDatabase("core");
            return coreDb.GetItem(CurrentUserSettingsPath) ?? coreDb.GetItem(AllUsersSettingsPath);
        }

        private Item GetSettingsDtoForSave()
        {
            Database currentDb = Factory.GetDatabase("core");
            string appSettingsPath = AppSettingsPath;
            Item currentUserItem = currentDb.GetItem(CurrentUserSettingsPath);
            if (currentUserItem == null)
            {
                Item settingsRootItem = currentDb.GetItem(appSettingsPath);
                if (settingsRootItem == null)
                {
                    return null;
                }
                Item folderTemplateItem = currentDb.GetItem(FolderTemplatePath);
                Item currentDomainItem = currentDb.CreateItemPath(appSettingsPath + CurrentDomain, folderTemplateItem,
                                                                  folderTemplateItem);
                Item defaultItem = currentDb.GetItem(appSettingsPath + IseSettingsItemAllUsers);
                currentUserItem = defaultItem.CopyTo(currentDomainItem, CurrentUserName);
            }
            return currentUserItem;
        }

        public void Save()
        {
            Item configuration = GetSettingsDtoForSave();
            if (configuration != null)
            {
                configuration.Edit(
                    p =>
                        {
                            configuration["PreScript"] = HttpUtility.HtmlEncode(Prescript);
                            configuration["LastScript"] = HttpUtility.HtmlEncode(LastScript);
                            ((CheckboxField) configuration.Fields["SaveLastScript"]).Checked = SaveLastScript;
                            ((CheckboxField) configuration.Fields["UseTypeInfo"]).Checked = UseTypeInfo;
                            configuration["HostWidth"] = HostWidth.ToString(CultureInfo.InvariantCulture);
                            configuration["ForegroundColor"] = ForegroundColor.ToString();
                            configuration["BackgroundColor"] = BackgroundColor.ToString();
                        });
            }
        }

        internal void Load()
        {
            Item configuration = GetSettingsDto();

            if (configuration != null)
            {
                Prescript = HttpUtility.HtmlDecode(configuration["PreScript"]);
                LastScript = HttpUtility.HtmlDecode(configuration["LastScript"]);
                SaveLastScript = ((CheckboxField) configuration.Fields["SaveLastScript"]).Checked;
                UseTypeInfo = ((CheckboxField) configuration.Fields["UseTypeInfo"]).Checked;
                int hostWidth;
                HostWidth = Int32.TryParse(configuration["HostWidth"], out hostWidth) ? hostWidth : 150;
                try
                {
                    ForegroundColor = (ConsoleColor) Enum.Parse(typeof (ConsoleColor), configuration["ForegroundColor"]);
                }
                catch (ArgumentException) // disregard parsing error & set default
                {
                    ForegroundColor = ConsoleColor.White;
                }

                try
                {
                    BackgroundColor = (ConsoleColor) Enum.Parse(typeof (ConsoleColor), configuration["BackgroundColor"]);
                }
                catch (ArgumentException) // disregard parsing error & set default
                {
                    BackgroundColor = ConsoleColor.DarkBlue;
                }
                Loaded = true;
            }
            else
            {
                Prescript = string.Empty;
                LastScript = string.Empty;
                SaveLastScript = false;
                UseTypeInfo = false;
                HostWidth = 80;
                ForegroundColor = ConsoleColor.White;
                BackgroundColor = ConsoleColor.DarkBlue;
                Loaded = true;
            }
        }
    }
}