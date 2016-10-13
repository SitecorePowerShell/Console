using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Cognifide.PowerShell.Core.Diagnostics;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Utility;
using Sitecore;
using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Security.Accounts;
using Sitecore.SecurityModel;

namespace Cognifide.PowerShell.Core.Settings
{
    public class ApplicationSettings
    {
        public const string SettingsItemPath = "/sitecore/system/Modules/PowerShell/Settings/";
        public const string IseSettingsItemAllUsers = "All Users";
        public const string FolderTemplatePath = "/sitecore/templates/Common/Folder";
        public const string ScriptLibraryPath = "/sitecore/system/Modules/PowerShell/Script Library/";
        public const string MediaLibraryPath = "/sitecore/media library/";
        public const string FontNamesPath = "/sitecore/system/Modules/PowerShell/Fonts/";
        private static string rulesDb;
        private static string settingsDb;
        private static string scriptLibraryDb;
        private const string LastScriptSettingFieldName = "LastScript";
        private const string SaveLastScriptSettingFieldName = "SaveLastScript";
        private const string LiveAutocompletionSettingFieldName = "LiveAutocompletion";
        private const string HostWidthSettingFieldName = "HostWidth";
        private const string ForegroundColorSettingFieldName = "ForegroundColor";
        private const string BackgroundColorSettingFieldName = "BackgroundColor";
        private const string FontSizeSettingFieldName = "FontSize";
        private const string FontFamilySettingFieldName = "FontFamily";

        private static readonly Dictionary<string, ApplicationSettings> instances =
            new Dictionary<string, ApplicationSettings>();

        private static readonly Regex validNameRegex = new Regex("[^a-zA-Z0-9]", RegexOptions.Compiled);

        private ApplicationSettings(string applicationName, bool personalizedSettings)
        {
            ApplicationName = applicationName;
            IsPersonalized = personalizedSettings;
        }

        public static string RulesDb
        {
            get
            {
                GetDatabaseName(ref rulesDb, "powershell/workingDatabase/rules");
                return rulesDb;
            }
        }

        public static string SettingsDb
        {
            get
            {
                GetDatabaseName(ref settingsDb, "powershell/workingDatabase/settings");
                return settingsDb;
            }
        }

        public static string ScriptLibraryDb
        {
            get
            {
                GetDatabaseName(ref scriptLibraryDb, "powershell/workingDatabase/scriptLibrary");
                return scriptLibraryDb;
            }
        }

        private bool Loaded { get; set; }
        public string LastScript { get; set; }
        public bool SaveLastScript { get; private set; }
        public int HostWidth { get; set; }
        public ConsoleColor ForegroundColor { get; set; }
        public ConsoleColor BackgroundColor { get; set; }
        public string ApplicationName { get; }
        public int FontSize { get; set; }
        private string FontFamily { get; set; }
        public bool IsPersonalized { get; }

        public string FontFamilyStyle
        {
            get
            {
                var db = Factory.GetDatabase(ScriptLibraryDb);
                var fonts = db.GetItem(FontNamesPath);
                var font = string.IsNullOrEmpty(FontFamily) ? "Monaco" : FontFamily;
                var fontItem = fonts.Children[font];
                return fontItem != null
                    ? fontItem["Phrase"]
                    : "Monaco, Menlo, \"Ubuntu Mono\", Consolas, source-code-pro, monospace";

            }
        }

        public bool LiveAutocompletion { get; set; }

        private string AppSettingsPath => SettingsItemPath + ApplicationName + "/";
        private string CurrentUserSettingsPath => AppSettingsPath + CurrentDomain + "/" + CurrentUserName;
        private string AllUsersSettingsPath => AppSettingsPath + IseSettingsItemAllUsers;

        private static string CurrentUserName => validNameRegex.Replace(User.Current.LocalName, "_");

        private static string CurrentDomain => validNameRegex.Replace(User.Current.Domain.Name, "_");

        private static void GetDatabaseName(ref string databaseName, string settingPath)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                databaseName = Factory.GetString(settingPath, false);
                if (string.IsNullOrEmpty(databaseName))
                {
                    databaseName = "master";
                }
            }
        }

        public static string GetSettingsPath(string applicationName, bool personalizedSettings)
        {
            return SettingsItemPath + GetSettingsName(applicationName, personalizedSettings);
        }

        private static string GetSettingsName(string applicationName, bool personalizedSettings)
        {
            return applicationName +
                   (personalizedSettings
                       ? "/" + CurrentDomain + "/" + CurrentUserName
                       : "/All Users");
        }

        public static void ReloadInstance(string applicationName, bool personalizedSettings)
        {
            var settingsPath = GetSettingsName(applicationName, personalizedSettings);
            lock (instances)
            {
                if (instances.ContainsKey(settingsPath))
                {
                    instances.Remove(settingsPath);
                }
            }
        }

        public static ApplicationSettings GetInstance(string applicationName, bool personalizedSettings = true)
        {
            var settingsPath = GetSettingsName(applicationName, personalizedSettings);
            ApplicationSettings instance = null;
            lock (instances)
            {
                if (instances.ContainsKey(settingsPath))
                {
                    instance = instances[settingsPath];
                }
                if (instance == null || !instance.Loaded)
                {
                    instance = new ApplicationSettings(applicationName, personalizedSettings);
                    instance.Load();
                    instances.Add(settingsPath, instance);
                }
            }
            return instance;
        }

        private Item GetSettingsDto()
        {
            var db = Factory.GetDatabase(SettingsDb);
            if (IsPersonalized)
            {
                return db?.GetItem(CurrentUserSettingsPath) ?? db?.GetItem(AllUsersSettingsPath);
            }
            return db?.GetItem(AllUsersSettingsPath);
        }

        private Item GetSettingsDtoForSave()
        {
            var db = Factory.GetDatabase(SettingsDb);
            var appSettingsPath = AppSettingsPath;
            using (new SecurityDisabler())
            {
                var currentUserItem = db.GetItem(CurrentUserSettingsPath);
                if (currentUserItem == null)
                {
                    var settingsRootItem = db.GetItem(appSettingsPath);
                    if (settingsRootItem == null)
                    {
                        return null;
                    }
                    var folderTemplateItem = db.GetItem(FolderTemplatePath);
                    var currentDomainItem = db.CreateItemPath(appSettingsPath + CurrentDomain, folderTemplateItem,
                        folderTemplateItem);
                    var defaultItem = db.GetItem(appSettingsPath + IseSettingsItemAllUsers);
                    currentUserItem = defaultItem.CopyTo(currentDomainItem, CurrentUserName);
                }
                return currentUserItem;
            }
        }

        public static Item GetIseMruContainerItem()
        {
            var currentUserItem = GetInstance(ApplicationNames.IseConsole).GetSettingsDtoForSave();
            var mruItem = currentUserItem.Children["MRU"] ??
                          currentUserItem.Add("MRU", new TemplateID(TemplateIDs.Folder));
            if (!mruItem.Publishing.NeverPublish)
            {
                mruItem.Edit(args => mruItem.Publishing.NeverPublish = true);
            }
            return mruItem;
        }

        public void Save()
        {
            var configuration = GetSettingsDtoForSave();
            if (configuration != null)
            {
                using (new SecurityDisabler())
                {
                    configuration.Edit(
                        p =>
                        {
                            configuration[LastScriptSettingFieldName] = HttpUtility.HtmlEncode(LastScript);
                            ((CheckboxField) configuration.Fields[SaveLastScriptSettingFieldName]).Checked = SaveLastScript;
                            ((CheckboxField)configuration.Fields[LiveAutocompletionSettingFieldName]).Checked = LiveAutocompletion;                            
                            configuration[HostWidthSettingFieldName] = HostWidth.ToString(CultureInfo.InvariantCulture);
                            configuration[ForegroundColorSettingFieldName] = ForegroundColor.ToString();
                            configuration[BackgroundColorSettingFieldName] = BackgroundColor.ToString();
                            configuration[FontSizeSettingFieldName] = FontSize.ToString();
                            configuration[FontFamilySettingFieldName] = FontFamily;
                            if (IsPersonalized)
                            {
                                configuration.Fields[FieldIDs.DisplayName].Reset();
                            }
                        });
                }
            }
        }

        internal void Load()
        {
            var configuration = GetSettingsDto();

            if (configuration != null)
            {
                try
                {
                    LastScript = TryGetSettingValue(LastScriptSettingFieldName,string.Empty,() => HttpUtility.HtmlDecode(configuration[LastScriptSettingFieldName]));
                    SaveLastScript =
                        TryGetSettingValue(SaveLastScriptSettingFieldName, true, () => ((CheckboxField) configuration.Fields[SaveLastScriptSettingFieldName]).Checked);
                    LiveAutocompletion =
                        TryGetSettingValue(LiveAutocompletionSettingFieldName, false,
                            () => ((CheckboxField) configuration.Fields[LiveAutocompletionSettingFieldName]).Checked);
                    HostWidth =
                        TryGetSettingValue(HostWidthSettingFieldName,150,
                            () =>
                            {
                                int hostWidth;
                                return int.TryParse(configuration[HostWidthSettingFieldName], out hostWidth) ? hostWidth : 150;
                            });
                    ForegroundColor =
                        TryGetSettingValue(ForegroundColorSettingFieldName, ConsoleColor.White,
                            () =>
                                (ConsoleColor)
                                    Enum.Parse(typeof(ConsoleColor), configuration[ForegroundColorSettingFieldName]));
                    BackgroundColor =
                        TryGetSettingValue(BackgroundColorSettingFieldName, ConsoleColor.DarkBlue,
                            () => (ConsoleColor) Enum.Parse(typeof (ConsoleColor), configuration[BackgroundColorSettingFieldName]));
                    FontSize =
                        TryGetSettingValue(FontSizeSettingFieldName, 12,
                            () =>
                            {
                                int fontSize;
                                return int.TryParse(configuration[FontSizeSettingFieldName], out fontSize)
                                    ? Math.Max(fontSize, 8)
                                    : 12;
                            });
                    FontFamily = TryGetSettingValue(FontFamilySettingFieldName, "Monaco",
                        () =>
                            string.IsNullOrWhiteSpace(configuration[FontFamilySettingFieldName])
                                ? "Monaco"
                                : configuration[FontFamilySettingFieldName]);

                    Loaded = true;
                }
                catch
                {
                    SetToDefault();
                }
            }
            else
            {
                SetToDefault();
            }
        }

        private void SetToDefault()
        {
            LastScript = string.Empty;
            SaveLastScript = true;
            LiveAutocompletion = false;
            HostWidth = 150;
            ForegroundColor = ConsoleColor.White;
            BackgroundColor = ConsoleColor.DarkBlue;
            FontSize = 12;
            FontFamily = "Monaco";
            Loaded = true;
        }

        private static T TryGetSettingValue<T>(string fieldName, T defaultValue, Func<T> action)
        {
            try
            {
                var result = action();
                return result;
            }
            catch (Exception ex)
            {
                PowerShellLog.Error($"Error while restoring setting {fieldName}", ex);
                return defaultValue;
            }
        }


        public static Item ScriptLibraryRoot
        {
            get
            {
                var db = Factory.GetDatabase(ScriptLibraryDb);
                return db.GetItem(ScriptLibraryPath);
            }
        }
    }
}