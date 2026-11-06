using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Security.Accounts;
using Sitecore.SecurityModel;
using Spe.Core.Diagnostics;
using Spe.Core.Extensions;

namespace Spe.Core.Settings
{
    public class ApplicationSettings
    {
        public const string IseSettingsItemAllUsers = "All Users";

        public static string SettingsItemPath => GetPathForItemId(ItemIDs.UserSettings, ref settingsItemPath);

        public const string ScriptLibraryPath = "/sitecore/system/Modules/PowerShell/Script Library/";
        public static string FontNamesPath => GetPathForItemId(ItemIDs.Fonts, ref fontNamesPath);

        public const string MediaLibraryPath = "/sitecore/media library/";
        public const string TemplatesPath = "/sitecore/templates/";
        private const string FolderIcon = "Office/32x32/folder.png";

        private const string LastScriptSettingFieldName = "LastScript";
        private const string SaveLastScriptSettingFieldName = "SaveLastScript";
        private const string ActiveTabsSettingFieldName = "ActiveTabs";
        private const string SaveActiveTabsSettingFieldName = "SaveActiveTabs";
        private const string LiveAutocompletionSettingFieldName = "LiveAutocompletion";
        private const string PerTabOutputSettingFieldName = "PerTabOutput"; 
        private const string HostWidthSettingFieldName = "HostWidth";
        private const string HostHeightSettingFieldName = "HostHeight";
        private const string ForegroundColorSettingFieldName = "ForegroundColor";
        private const string BackgroundColorSettingFieldName = "BackgroundColor";
        private const string FontSizeSettingFieldName = "FontSize";
        private const string FontFamilySettingFieldName = "FontFamily";
        private const string MostRecentlyUsedScriptsFieldName = "MostRecentlyUsedScripts";

        private static readonly Dictionary<string, ApplicationSettings> instances = new();

        private static readonly Regex validNameRegex = new Regex("[^a-zA-Z0-9]", RegexOptions.Compiled);

        private static string settingsDb;
        private static string scriptLibraryDb;
        private static string rulesDb;
        private static string settingsItemPath;
        private static string fontNamesPath;
        private static Item scriptLibraryRoot;

        private ApplicationSettings(string applicationName, bool personalizedSettings)
        {
            ApplicationName = applicationName;
            IsPersonalized = personalizedSettings;
        }

        public static string RulesDb => GetDatabaseName(ref rulesDb, "powershell/workingDatabase/rules");

        public static string SettingsDb => GetDatabaseName(ref settingsDb, "powershell/workingDatabase/settings");

        public static string ScriptLibraryDb => GetDatabaseName(ref scriptLibraryDb, "powershell/workingDatabase/scriptLibrary");

        private bool Loaded { get; set; }
        public string LastScript { get; set; }
        public bool SaveLastScript { get; private set; }
        public bool SaveActiveTabs { get; private set; }
        public string ActiveTabs { get; set; }
        public string MostRecentlyUsedScripts { get; set; }
        public int HostWidth { get; set; }
        public int HostHeight { get; set; }
        public ConsoleColor ForegroundColor { get; set; }
        public ConsoleColor BackgroundColor { get; set; }
        public string ApplicationName { get; }
        public int FontSize { get; set; }
        private string FontFamily { get; set; }
        public bool IsPersonalized { get; }

        private string fontFamilyStyle;
        public string FontFamilyStyle
        {
            get
            {
                if (fontFamilyStyle != null) return fontFamilyStyle;
                var db = Factory.GetDatabase(ScriptLibraryDb);
                var fonts = db.GetItem(ItemIDs.Fonts);
                var font = string.IsNullOrEmpty(FontFamily) ? "Monaco" : FontFamily;
                var fontItem = fonts.Children[font];
                fontFamilyStyle = fontItem != null
                    ? fontItem["Phrase"]
                    : "Monaco, Menlo, \"Ubuntu Mono\", Consolas, source-code-pro, monospace";
                return fontFamilyStyle;
            }
        }

        public bool LiveAutocompletion { get; set; }
        public bool PerTabOutput { get; set; }

        private string AppSettingsPath => SettingsItemPath + ApplicationName + "/";
        private string CurrentUserSettingsPath => AppSettingsPath + CurrentDomain + "/" + CurrentUserName;
        private string AllUsersSettingsPath => AppSettingsPath + IseSettingsItemAllUsers;

        private static string CurrentUserName => validNameRegex.Replace(User.Current.LocalName, "_");

        private static string CurrentDomain => User.Current?.Domain != null ? validNameRegex.Replace(User.Current?.Domain?.Name, "_") : string.Empty;

        private static string GetDatabaseName(ref string databaseName, string settingPath)
        {
            if (!string.IsNullOrEmpty(databaseName)) return databaseName;

            databaseName = Factory.GetString(settingPath, false);
            if (string.IsNullOrEmpty(databaseName))
            {
                databaseName = "master";
            }

            return databaseName;
        }
        
        private static string GetPathForItemId(ID itemId, ref string itemPath)
        {
            if (itemPath != null) return itemPath;
            var db = Factory.GetDatabase(ScriptLibraryDb);
            var item = db?.GetItem(itemId);
            if (item == null) return null;
            itemPath = item.Paths.Path + "/";
            return itemPath;
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
                instances.TryGetValue(settingsPath, out instance) ;
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
                    var folderTemplateItem = db.GetItem(TemplateIDs.Folder);
                    var currentDomainItem = db.CreateItemPath(appSettingsPath + CurrentDomain, folderTemplateItem,
                        folderTemplateItem);
                    currentDomainItem.Edit(args => currentDomainItem.Appearance.Icon = FolderIcon);
                    var defaultItem = db.GetItem(appSettingsPath + IseSettingsItemAllUsers);
                    currentUserItem = defaultItem.CopyTo(currentDomainItem, CurrentUserName);
                }
                return currentUserItem;
            }
        }

        public void Save()
        {
            var configuration = GetSettingsDtoForSave();
            if (configuration == null) return;

            using (new SecurityDisabler())
            {
                configuration.Edit(
                    p =>
                    {
                        configuration[LastScriptSettingFieldName] = HttpUtility.HtmlEncode(LastScript);
                        ((CheckboxField) configuration.Fields[SaveLastScriptSettingFieldName]).Checked = SaveLastScript;
                        ((CheckboxField) configuration.Fields[SaveActiveTabsSettingFieldName]).Checked = SaveActiveTabs;
                        configuration[ActiveTabsSettingFieldName] = ActiveTabs;
                        configuration[MostRecentlyUsedScriptsFieldName] = MostRecentlyUsedScripts;
                        ((CheckboxField)configuration.Fields[LiveAutocompletionSettingFieldName]).Checked = LiveAutocompletion;
                        ((CheckboxField)configuration.Fields[PerTabOutputSettingFieldName]).Checked = PerTabOutput;
                        configuration[HostWidthSettingFieldName] = HostWidth.ToString(CultureInfo.InvariantCulture);
                        configuration[HostHeightSettingFieldName] = HostHeight.ToString(CultureInfo.InvariantCulture);
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
                    SaveActiveTabs =
                        TryGetSettingValue(SaveActiveTabsSettingFieldName, false,
                            () => ((CheckboxField) configuration.Fields[SaveActiveTabsSettingFieldName]).Checked);
                    ActiveTabs = TryGetSettingValue(ActiveTabsSettingFieldName, string.Empty,
                        () => configuration[ActiveTabsSettingFieldName]);
                    MostRecentlyUsedScripts = TryGetSettingValue(MostRecentlyUsedScriptsFieldName, string.Empty,
                        () => configuration[MostRecentlyUsedScriptsFieldName]);
                    LiveAutocompletion =
                        TryGetSettingValue(LiveAutocompletionSettingFieldName, false,
                            () => ((CheckboxField) configuration.Fields[LiveAutocompletionSettingFieldName]).Checked);
                    PerTabOutput = 
                    TryGetSettingValue(PerTabOutputSettingFieldName, false,
                        () => ((CheckboxField) configuration.Fields[PerTabOutputSettingFieldName]).Checked);
                    HostWidth =
                        TryGetSettingValue(HostWidthSettingFieldName,150,
                            () => int.TryParse(configuration[HostWidthSettingFieldName], out var hostWidth) ? hostWidth : 150);
                    HostHeight =
                        TryGetSettingValue(HostHeightSettingFieldName,int.MaxValue,
                            () => int.TryParse(configuration[HostHeightSettingFieldName], out var hostHeight) ? hostHeight : int.MaxValue);
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
                            () => int.TryParse(configuration[FontSizeSettingFieldName], out var fontSize)
                                ? Math.Max(fontSize, 8)
                                : 12);
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
            SaveActiveTabs = false;
            ActiveTabs = string.Empty;
            LiveAutocompletion = false;
            PerTabOutput = false;
            HostWidth = 150;
            HostHeight = int.MaxValue;
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
                PowerShellLog.Error($"[Settings] action=restoreFailed field={fieldName}", ex);
                return defaultValue;
            }
        }
        
        public static Item ScriptLibraryRoot
        {
            get
            {
                if (scriptLibraryRoot != null) return scriptLibraryRoot;
                var db = Factory.GetDatabase(ScriptLibraryDb);
                scriptLibraryRoot = db.GetItem(ScriptLibraryPath);
                return scriptLibraryRoot;
            }
        }
    }
}