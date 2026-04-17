using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.IO;
using Sitecore.Jobs;
using Sitecore.Mvc.Extensions;
using Sitecore.Resources;
using Sitecore.Security;
using Sitecore.Security.Accounts;
using Sitecore.Shell.Controls.Splitters;
using Sitecore.Shell.Framework;
using Sitecore.Shell.Framework.Commands;
using Sitecore.StringExtensions;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;
using Sitecore.Web.UI.WebControls.Ribbons;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Spe.Client.Controls;
using Spe.Core.Diagnostics;
using Spe.Core.Extensions;
using Spe.Core.Host;
using Spe.Core.Settings;
using Spe.Core.Settings.Authorization;
using Spe.Core.VersionDecoupling;
using Literal = Sitecore.Web.UI.HtmlControls.Literal;

namespace Spe.Client.Applications
{
    public class PowerShellIse : BaseForm, IHasCommandContext, IPowerShellRunner
    {
        public const string DefaultSessionName = "ISE_Editing_Session";
        public const string DefaultUser = "CurrentUser";
        public const string DefaultLanguage = "CurrentLanguage";

        protected Memo Editor;
        protected Memo OpenedScripts;
        protected Memo ScriptItemIdMemo;
        protected Memo ScriptItemDbMemo;
        protected Memo ActiveTabsMemo;
        protected Memo TerminalCommand;
        protected Literal Progress;
        protected Border ProgressOverlay;
        protected Border RibbonPanel;
        protected Border ScriptResult;
        protected Memo SelectionText;
        protected Memo Breakpoints;
        protected Border ElevationRequiredPanel;
        protected Border ElevatedPanel;
        protected Border ElevationBlockedPanel;
        protected Border InfoPanel;
        protected Tabstrip Tabs;
        protected Border TabsPanel;
        protected TreeviewEx ContentTreeview;
        protected DataContext ContentDataContext;
        protected VSplitterXmlControl VSplitter;
 
        public bool Debugging { get; set; }
        public bool InBreakpoint { get; set; }

        protected bool ScriptRunning
        {
            get => StringUtil.GetString(Context.ClientPage.ServerProperties["ScriptRunning"]) == "1";
            set => Context.ClientPage.ServerProperties["ScriptRunning"] = value ? "1" : string.Empty;
        }

        protected bool WasElevated
        {
            get => StringUtil.GetString(Context.ClientPage.ServerProperties["WasElevated"]) == "1";
            set => Context.ClientPage.ServerProperties["WasElevated"] = value ? "1" : string.Empty;
        }

        public string ParentFrameName
        {
            get => StringUtil.GetString(ServerProperties["ParentFrameName"]);
            set => ServerProperties["ParentFrameName"] = value;
        }

        // Index of the next unread line in session.Output for this ISE
        // page's streaming drain. All lines at indices < CommittedLineCount
        // have been sent to the client as committed (non-partial) lines.
        // When HasPendingPartial is true, the line at CommittedLineCount
        // is the currently-open partial on the client - it may still grow
        // on the server and will be re-read on the next poll.
        private int CommittedLineCount
        {
            get => int.TryParse(StringUtil.GetString(ServerProperties["OutputCommittedLineCount"]), out var v) ? v : 0;
            set => ServerProperties["OutputCommittedLineCount"] = value.ToString();
        }

        private bool HasPendingPartial
        {
            get => StringUtil.GetString(ServerProperties["OutputHasPendingPartial"]) == "1";
            set => ServerProperties["OutputHasPendingPartial"] = value ? "1" : string.Empty;
        }

        public string ScriptItemId
        {
            get => ScriptItemIdMemo.Value;
            set
            {
                if (value.IsWhiteSpaceOrNull())
                {
                    Log.Error("ScriptItemId is null or empty", this);
                }

                ScriptItemIdMemo.Value = value;
            }
        }

        public string ScriptItemDb
        {
            get => ScriptItemDbMemo.Value;
            set
            {
                if (value.IsWhiteSpaceOrNull())
                {
                    Log.Error("ScriptItemDb is null or empty", this);
                }

                ScriptItemDbMemo.Value = value;
            }
        }

        public static string ContextItemId
        {
            get => StringUtil.GetString(Context.ClientPage.ServerProperties["ContextItemID"]);
            set => Context.ClientPage.ServerProperties["ContextItemID"] = value;
        }

        public static string ContextItemDb
        {
            get => StringUtil.GetString(Context.ClientPage.ServerProperties["ContextItemDb"]);
            set => Context.ClientPage.ServerProperties["ContextItemDb"] = value;
        }

        public static bool ScriptModified
        {
            get => StringUtil.GetString(Context.ClientPage.ServerProperties["ScriptModified"]) == "1";
            set => Context.ClientPage.ServerProperties["ScriptModified"] = value ? "1" : string.Empty;
        }

        public static int TabSequencer
        {
            get => (int)(Context.ClientPage.ServerProperties["TabSequencer"] ?? 0);
            set => Context.ClientPage.ServerProperties["TabSequencer"] = value;
        }

        public static bool UseContext
        {
            get => string.IsNullOrEmpty(StringUtil.GetString(Context.ClientPage.ServerProperties["UseContext"]));
            set => Context.ClientPage.ServerProperties["UseContext"] = value ? string.Empty : "0";
        }

        public Item ScriptItem
        {
            get
            {
                var scriptItemId = ScriptItemId;
                return string.IsNullOrEmpty(scriptItemId)
                    ? null
                    : Factory.GetDatabase(ScriptItemDb).GetItem(new ID(scriptItemId));
            }
        }

        public static Item ContextItem
        {
            get
            {
                var contextItemId = ContextItemId;
                return string.IsNullOrEmpty(contextItemId)
                    ? null
                    : Factory.GetDatabase(ContextItemDb).GetItem(new ID(contextItemId),
                        CurrentLanguage == DefaultLanguage ? Sitecore.Context.Language : Language.Parse(CurrentLanguage));
            }
        }

        public static string CurrentSessionId
        {
            get => StringUtil.GetString(Context.ClientPage.ServerProperties["CurrentSessionId"]);
            set => Context.ClientPage.ServerProperties["CurrentSessionId"] = value;
        }

        public static string CurrentUser
        {
            get => StringUtil.GetString(Context.ClientPage.ServerProperties["CurrentUser"]);
            set => Context.ClientPage.ServerProperties["CurrentUser"] = value;
        }

        public static string CurrentLanguage
        {
            get => StringUtil.GetString(Context.ClientPage.ServerProperties["CurrentLanguage"]);
            set => Context.ClientPage.ServerProperties["CurrentLanguage"] = value;
        }

        // Holds the SELECTED policy ITEM ID (GUID string, braces included) - not
        // the policy name. Names aren't unique across subfolders, so looking
        // policies up by name can pick the wrong one in the ISE. The ribbon
        // button still displays the human-readable name via IsePolicyPanel.
        public static string CurrentPolicy
        {
            get => StringUtil.GetString(Context.ClientPage.ServerProperties["CurrentPolicy"]);
            set => Context.ClientPage.ServerProperties["CurrentPolicy"] = value ?? string.Empty;
        }

        private static Item ResolveCurrentPolicyItem() =>
            RemotingPolicyManager.ResolvePolicyItem(CurrentPolicy);

        public SpeJobMonitor Monitor { get; private set; }
        public SpeJobMonitor AuxiliaryMonitor { get; private set; }

        public CommandContext GetCommandContext()
        {
            var itemNotNull = Sitecore.Client.CoreDatabase.GetItem("{FDD5B2D5-31BE-41C3-AA76-64E5CC63B187}");
            // /sitecore/content/Applications/PowerShell/PowerShellIse/Ribbon
            var context = new CommandContext { RibbonSourceUri = itemNotNull.Uri };
            return context;
        }

        public bool MonitorActive
        {
            get => Monitor.Active;
            set => Monitor.Active = value;
        }

        private static bool IsHackedParameter(string parameter)
        {
            var xssCleanup =
                new Regex(@"<script[^>]*>[\s\S]*?</script>|<noscript[^>]*>[\s\S]*?</noscript>|<img.*onerror.*>");
            if (xssCleanup.IsMatch(parameter))
            {
                return true;
            }

            return false;
        }

        protected override void OnLoad(EventArgs e)
        {
            if (!SecurityHelper.CanRunApplication("PowerShell/PowerShellIse") ||
                ServiceAuthorizationManager.TerminateUnauthorizedRequest(WebServiceSettings.ServiceClient,
                    Context.User.Name))
            {
                PowerShellLog.Audit($"[ISE] action=accessDenied target=ISE");
                return;
            }

            base.OnLoad(e);

            if (Monitor == null)
            {
                if (!Context.ClientPage.IsEvent)
                {
                    Monitor = new SpeJobMonitor { ID = "Monitor" };
                    Context.ClientPage.Controls.Add(Monitor);
                }
                else
                {
                    Monitor = (SpeJobMonitor)Context.ClientPage.FindControl("Monitor");
                }
            }

            // Auxiliary monitor for side jobs (e.g. the policy editor/creator)
            // that need to pump dialog messages to the ISE page without touching
            // the terminal. Their job output goes to a throwaway session that the
            // main PrintOutput handler doesn't know about, and MonitorOnJobFinished
            // (which drains output and resets ISE UI state) only fires on the main
            // Monitor, leaving the terminal prompt and ribbon undisturbed.
            if (AuxiliaryMonitor == null)
            {
                if (!Context.ClientPage.IsEvent)
                {
                    AuxiliaryMonitor = new SpeJobMonitor { ID = "AuxiliaryMonitor" };
                    Context.ClientPage.Controls.Add(AuxiliaryMonitor);
                }
                else
                {
                    AuxiliaryMonitor = (SpeJobMonitor)Context.ClientPage.FindControl("AuxiliaryMonitor");
                }
            }

            Monitor.JobFinished += MonitorOnJobFinished;
            AuxiliaryMonitor.JobFinished += AuxiliaryMonitorOnJobFinished;

            Tabs.OnChange += TabsOnChange;
            if (Context.ClientPage.IsEvent)
                return;
            
            var settings = ApplicationSettings.GetInstance(ApplicationNames.ISE);

            var hasActiveTabsState = settings.SaveActiveTabs && !string.IsNullOrEmpty(settings.ActiveTabs);
            if (settings.SaveLastScript && !hasActiveTabsState)
            {
                Editor.Value = settings.LastScript;
            }

            var itemId = WebUtil.GetQueryString("id");
            var itemDb = WebUtil.GetQueryString("db");

            if (!itemId.IsWhiteSpaceOrNull())
            {
                ScriptItemId = itemId;
                ScriptItemDb = itemDb;
            }

            ContextItemDb = Context.ContentDatabase.Name;
            var contextItem = Context.ContentDatabase.GetItem(Context.Site.ContentStartPath) ??
                              UIUtil.GetHomeItem(Context.User);
            ContextItemId = contextItem?.ID.ToString() ?? String.Empty;

            CurrentSessionId = DefaultSessionName;
            CurrentUser = DefaultUser;
            CurrentLanguage = DefaultLanguage;
            ParentFrameName = WebUtil.GetQueryString("pfn");
            UpdateRibbon();
            
            if(itemDb.IsWhiteSpaceOrNull())
            {
                itemDb = Context.ContentDatabase.Name;
            }
            ContentDataContext.GetFromQueryString();
            ContentDataContext.BeginUpdate();
            ContentDataContext.Parameters = $"databasename={itemDb}";
            ContentDataContext.Database = itemDb;
            ContentTreeview.RefreshRoot();
            ContentDataContext.Root = ApplicationSettings.ScriptLibraryRoot.ID.ToString();

            if (!ScriptItemId.IsNullOrEmpty() && !ScriptItemDb.IsNullOrEmpty())
            {
                ContentDataContext.SetFolder(ScriptItem.Uri);
            }

            ContentDataContext.EndUpdate();
            ContentTreeview.RefreshRoot();
        }

        private void TabsOnChange(object sender, EventArgs e)
        {
            var tab = Tabs.Controls.OfType<Tab>().Skip(Tabs.Active).FirstOrDefault();
            SelectTabByIndex(Tabs.Active + 1);

            // Use the ID-based ScriptItem lookup (populated from the per-tab memos)
            // instead of reconstructing the path from OpenedScripts - that path may
            // be malformed for scripts opened from locations outside ScriptLibraryPath
            // (e.g. via a search gallery), which previously caused a NullReferenceException.
            var scriptItem = ScriptItem;
            if (scriptItem != null)
            {
                ContentDataContext.BeginUpdate();
                ContentDataContext.SetFolder(scriptItem.Uri);
                ContentDataContext.EndUpdate();
                ContentTreeview.SetSelectedItem(scriptItem);
                ContentTreeview.RefreshRoot();
                ContentTreeview.RefreshSelected();
            }
            else
            {
                Item selectionItem = ContentTreeview.GetSelectionItem();
                ContentTreeview.SelectedIDs.Clear();
                if (selectionItem != null)
                    ContentTreeview.Refresh(selectionItem);
            }

            // Persist the new active tab so reopening the ISE restores it.
            TrySaveActiveTabs();
        }

        public override void HandleMessage(Message message)
        {
            Error.AssertObject(message, "message");
            base.HandleMessage(message);

            var item = ScriptItem;
            var context = new CommandContext(item);
            foreach (var key in message.Arguments.AllKeys)
            {
                context.Parameters.Add(key, message.Arguments[key]);
            }

            if (!string.IsNullOrEmpty(ParentFrameName))
            {
                context.Parameters["ParentFramename"] = ParentFrameName;
            }

            if (context.Parameters.AllKeys.Contains("currLang"))
            {
                context.Parameters["currLang"] = CurrentLanguage == DefaultLanguage
                    ? Context.Language.Name
                    : CurrentLanguage;
            }

            Dispatcher.Dispatch(message, context);
        }

        [HandleMessage("ise:updatetreeview", true)]
        protected void UpdateTreeView(ClientPipelineArgs args)
        {
            TabsOnChange(null,null);
        }

        [HandleMessage("ise:open", true)]
        protected void Open(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (args.IsPostBack)
            {
                if (!args.HasResult)
                    return;
                var path = args.Result.Split(':');
                LoadItem(path[0], path[1]);
                UpdateRibbon();
            }
            else
            {
                const string icon = "powershell/48x48/script.png";
                const string root = ApplicationSettings.ScriptLibraryPath;
                const string selected = ApplicationSettings.ScriptLibraryPath;

                var str = selected;
                if (selected.EndsWith("/"))
                {
                    var obj = Context.ContentDatabase.Items[StringUtil.Left(selected, selected.Length - 1)];
                    if (obj != null)
                        str = obj.ID.ToString();
                }

                var urlString = new UrlString(UIUtil.GetUri("control:PowerShellScriptBrowser"));
                urlString.Append("id", selected);
                urlString.Append("fo", str);
                urlString.Append("ro", root);
                urlString.Append("he", Texts.PowerShellIse_Open_Open_Script);
                urlString.Append("txt", Texts.PowerShellIse_Open_Select_the_script_item_that_you_want_to_open_);
                urlString.Append("ic", icon);
                urlString.Append("btn", Sitecore.Texts.OPEN);
                urlString.Append("opn", "1");
                SheerResponse.ShowModalDialog(urlString.ToString(), true);
                args.WaitForPostBack();
            }
        }

        [HandleMessage("ise:mruopen", true)]
        protected void MruOpen(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            var db = args.Parameters["db"];
            var id = args.Parameters["id"];
            var item = Database.GetDatabase(db).GetItem(id);
            if(item.IsPowerShellScript()){
                LoadItem(db, id);
            }
        }

        protected void ContentTreeview_Click()
        {
            var folder = ContentDataContext.GetFolder();
            if (folder.IsPowerShellScript())
            {
                LoadItem(folder.Database.Name, folder.ID.ToString());
            }
        }

        [HandleMessage("ise:changecontextaccount", true)]
        protected void SecurityChangeContextAccount(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            args.CarryResultToNextProcessor = false;
            args.AbortPipeline();
        }

        [HandleMessage("item:load", true)]
        protected void LoadContentEditor(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            var parameters = new UrlString();
            parameters.Add("id", args.Parameters["id"]);
            parameters.Add("fo", args.Parameters["id"]);
            Sitecore.Shell.Framework.Windows.RunApplication("Content Editor", parameters.ToString());
        }

        protected void MruUpdate(Item scriptItem)
        {
            Assert.ArgumentNotNull(scriptItem, "scriptItem");

            var db = scriptItem.Database.Name;
            var id = scriptItem.ID.ToString();

            var settings = ApplicationSettings.GetInstance(ApplicationNames.ISE);
            var entries = ParseMruEntries(settings.MostRecentlyUsedScripts);

            // Remove any existing entry for this script, then prepend it.
            entries.RemoveAll(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase)
                                   && string.Equals(e.Db, db, StringComparison.OrdinalIgnoreCase));
            entries.Insert(0, new MruEntry { Id = id, Db = db });

            // Keep only the 10 most recent.
            if (entries.Count > 10)
            {
                entries = entries.Take(10).ToList();
            }

            settings.MostRecentlyUsedScripts = SerializeMruEntries(entries);
            settings.Save();
        }

        public class MruEntry
        {
            public string Id { get; set; }
            public string Db { get; set; }
        }

        public static System.Collections.Generic.List<MruEntry> ParseMruEntries(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new System.Collections.Generic.List<MruEntry>();
            }
            try
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.List<MruEntry>>(json)
                       ?? new System.Collections.Generic.List<MruEntry>();
            }
            catch
            {
                return new System.Collections.Generic.List<MruEntry>();
            }
        }

        public static string SerializeMruEntries(System.Collections.Generic.List<MruEntry> entries)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(entries);
        }

        [HandleMessage("ise:new", true)]
        protected void NewScript(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            ScriptItemId = string.Empty;
            ScriptItemDb = string.Empty;
            Editor.Value = string.Empty;
            CreateNewTab(null);
            UpdateRibbon();
            TrySaveActiveTabs();
        }

        [HandleMessage("ise:saveas", true)]
        protected void SaveAs(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            args.Parameters["message"] = "ise:saveas";
            if (!RequestSessionElevationEx(args, ApplicationNames.ItemSave, SessionElevationManager.SaveAction))
            {
                return;
            }

            if (args.IsPostBack)
            {
                if (!args.HasResult)
                    return;

                var path = args.Result.Split(':');
                var db = Factory.GetDatabase(path[0]);
                var itemTemplate = db.GetTemplate(Templates.Script.Id);
                var libraryTemplate = db.GetTemplate(Templates.ScriptLibrary.Id);
                var scriptItem = db.CreateItemPath(path[1], libraryTemplate, itemTemplate);
                ScriptItemId = scriptItem.ID.ToString();
                ScriptItemDb = scriptItem.Database.Name;
                SaveItem(new ClientPipelineArgs());
                MruUpdate(scriptItem);
                UpdateRibbon();
            }
            else
            {
                const string icon = "powershell/48x48/script.png";
                const string root = ApplicationSettings.ScriptLibraryPath;
                const string selected = ApplicationSettings.ScriptLibraryPath;

                var str = selected;
                if (selected.EndsWith("/"))
                {
                    var obj = Context.ContentDatabase.Items[StringUtil.Left(selected, selected.Length - 1)];
                    if (obj != null)
                        str = obj.ID.ToString();
                }

                var urlString = new UrlString(UIUtil.GetUri("control:PowerShellScriptBrowser"));
                urlString.Append("id", selected);
                urlString.Append("fo", str);
                urlString.Append("ro", root);
                urlString.Append("he", Texts.PowerShellIse_SaveAs_Select_Script_Library);
                urlString.Append("txt",
                    Texts.PowerShellIse_SaveAs_Select_the_Library_that_you_want_to_save_your_script_to_);
                urlString.Append("ic", icon);
                urlString.Append("btn", Sitecore.Texts.SELECT);
                SheerResponse.ShowModalDialog(urlString.ToString(), true);
                args.WaitForPostBack();
            }
        }

        [HandleMessage("ise:save", true)]
        [HandleMessage("ise:handlesave", true)]
        protected void SaveItem(ClientPipelineArgs args)
        {
            if (string.IsNullOrEmpty(ScriptItemId))
            {
                SaveAs(args);
            }
            else
            {
                args.Parameters["message"] = "ise:save";
                if (!RequestSessionElevationEx(args, ApplicationNames.ItemSave, SessionElevationManager.SaveAction))
                {
                    return;
                }

                var scriptItem = ScriptItem;
                if (scriptItem == null)
                    return;
                scriptItem.Edit(
                    editArgs => { scriptItem.Fields[Templates.Script.Fields.ScriptBody].Value = Editor.Value; });
                SheerResponse.Eval("spe.updateModificationFlag(true);");
                var tabIndex = Tabs.Active + 1;
                UpdateTabInfo(scriptItem, tabIndex);
            }
        }

        [HandleMessage("ise:loadinitialscript", true)]
        protected void LoadInitialScript(ClientPipelineArgs args)
        {
            if (ScriptItemId.Length > 0)
            {
                LoadItem(ScriptItemDb, ScriptItemId);
            }
            else
            {
                CreateNewTab(null);
            }
        }

        [HandleMessage("ise:restoreactivetabs", true)]
        protected void RestoreActiveTabs(ClientPipelineArgs args)
        {
            var settings = ApplicationSettings.GetInstance(ApplicationNames.ISE);
            if (!settings.SaveActiveTabs || string.IsNullOrEmpty(settings.ActiveTabs))
            {
                LoadInitialScript(args);
                return;
            }

            try
            {
                var tabState = Newtonsoft.Json.Linq.JObject.Parse(settings.ActiveTabs);
                var tabs = tabState["tabs"] as Newtonsoft.Json.Linq.JArray;
                if (tabs == null || tabs.Count == 0)
                {
                    LoadInitialScript(args);
                    return;
                }

                if (ScriptItemId.Length > 0)
                {
                    LoadInitialScript(args);
                    return;
                }

                var activeIndex = (int?)tabState["activeIndex"] ?? 1;
                var restoreInfo = new Newtonsoft.Json.Linq.JArray();

                // Don't rely on Editor.Value to carry per-tab content through the
                // loop - it's a single memo and only the last value survives to the
                // client. Instead create tabs with a placeholder and push all content
                // via a single JS call at the end.
                Editor.Value = string.Empty;

                foreach (var tabToken in tabs)
                {
                    var db = (string)tabToken["db"] ?? string.Empty;
                    var id = (string)tabToken["id"] ?? string.Empty;
                    var modified = (bool?)tabToken["modified"] ?? false;
                    var content = (string)tabToken["content"];
                    var tabInfo = new Newtonsoft.Json.Linq.JObject
                    {
                        ["modified"] = modified,
                        ["deleted"] = false
                    };

                    if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(db))
                    {
                        var scriptItem = Factory.GetDatabase(db).GetItem(id);
                        if (scriptItem != null && scriptItem.IsPowerShellScript())
                        {
                            CreateNewTab(scriptItem);
                            ScriptItemId = scriptItem.ID.ToString();
                            ScriptItemDb = scriptItem.Database.Name;

                            tabInfo["content"] = modified && content != null
                                ? content
                                : scriptItem[Templates.Script.Fields.ScriptBody];
                        }
                        else
                        {
                            // Item was deleted - create tab with stored content
                            CreateNewTab(null);
                            tabInfo["deleted"] = true;
                            tabInfo["content"] = content ?? string.Empty;
                        }
                    }
                    else
                    {
                        // Untitled tab
                        CreateNewTab(null);
                        tabInfo["content"] = content ?? string.Empty;
                    }

                    restoreInfo.Add(tabInfo);
                }

                var tabCount = Tabs.Controls.Count;
                SelectTabByIndex(Math.Min(activeIndex, tabCount));

                var restoreData = new Newtonsoft.Json.Linq.JObject
                {
                    ["tabs"] = restoreInfo
                };
                var escapedJson = HttpUtility.JavaScriptStringEncode(restoreData.ToString(Newtonsoft.Json.Formatting.None));
                SheerResponse.Eval($"spe.applyRestoredTabs(JSON.parse(\"{escapedJson}\"));");
            }
            catch (Exception ex)
            {
                PowerShellLog.Error($"Failed to restore active tabs: {ex.Message}", ex);
                LoadInitialScript(args);
            }
        }

        [HandleMessage("ise:savetabstate", true)]
        protected void SaveTabState(ClientPipelineArgs args)
        {
            var settings = ApplicationSettings.GetInstance(ApplicationNames.ISE);
            if (!settings.SaveActiveTabs) return;

            settings.Load();
            settings.ActiveTabs = ActiveTabsMemo.Value;
            settings.Save();
        }

        /// <summary>
        ///     Triggers the client to serialize the current tab set and post it back
        ///     via ise:savetabstate. Called after any server-side mutation that
        ///     changes the tab set (open, close, switch, execute) so the stored
        ///     state is always current without relying on window-unload delivery.
        /// </summary>
        private void TrySaveActiveTabs()
        {
            var settings = ApplicationSettings.GetInstance(ApplicationNames.ISE);
            if (settings.SaveActiveTabs)
            {
                SheerResponse.Eval("spe.saveActiveTabs();");
            }
        }

        [HandleMessage("ise:reload", true)]
        protected void ReloadItem(ClientPipelineArgs args)
        {
            if (string.IsNullOrEmpty(ScriptItemId) || string.IsNullOrEmpty(ScriptItemDb))
                return;

            var scriptItem = Factory.GetDatabase(ScriptItemDb).GetItem(ScriptItemId);
            if (scriptItem == null || !scriptItem.IsPowerShellScript())
                return;

            var content = scriptItem[Templates.Script.Fields.ScriptBody] ?? string.Empty;
            Editor.Value = content;
            var escaped = HttpUtility.JavaScriptStringEncode(content);
            SheerResponse.Eval($"spe.reloadCurrentEditor(\"{escaped}\");");
        }

        private void LoadItem(string db, string id)
        {
            Assert.ArgumentNotNull(id, "id");
            Assert.ArgumentNotNull(db, "db");
            var scriptItem = Factory.GetDatabase(db).GetItem(id);

            if (!scriptItem.IsPowerShellScript())
            {
                SessionElevationErrors.OperationFailedWrongDataTemplate();
                return;
            }

            if (scriptItem[Templates.Script.Fields.ScriptBody] != null)
            {
                Editor.Value = scriptItem[Templates.Script.Fields.ScriptBody];
                var createdNew = CreateNewTab(scriptItem);

                ScriptItemId = scriptItem.ID.ToString();
                ScriptItemDb = scriptItem.Database.Name;
                MruUpdate(scriptItem);
                UpdateRibbon();
                SheerResponse.Eval("scForm.postRequest(\"\", \"\", \"\", \"ise:updatetreeview\");");
                TrySaveActiveTabs();
            }
            else
            {
                SheerResponse.Alert(Texts.PowerShellIse_LoadItem_The_item_is_not_a_script_, true);
            }
        }

        private bool CreateNewTab(Item scriptItem)
        {
            var itemEditing = scriptItem != null;
            var path = itemEditing
                ? scriptItem.Paths.Path.Substring(ApplicationSettings.ScriptLibraryPath.Length)
                : String.Empty;
            var tabIndex = 0;
            var searchPath = $"{path}:";
            var openedScript = OpenedScripts.Value.Split('\n').Select(line => line.Trim())
                .FirstOrDefault(line => line.StartsWith(searchPath, StringComparison.OrdinalIgnoreCase));
            var scriptAlreadyOpened = itemEditing && openedScript != null;
            if (scriptAlreadyOpened)
            {
                tabIndex = int.Parse(openedScript.Split(':')[1]);
            }
            else
            {
                TabSequencer++;
                SheerResponse.Eval($"spe.createEditor({TabSequencer});");
                var newTab = new Tab
                    { Header = $"{Tabs.Controls.Count + 1}", Active = true };
                Tabs.Controls.Add(newTab);
                tabIndex = Tabs.Controls.Count;
                UpdateTabInfo(scriptItem, tabIndex);
            }

            SelectTabByIndex(tabIndex);
            if (!scriptAlreadyOpened)
            {
                SheerResponse.Eval("spe.updateEditor();");
            }
            return !scriptAlreadyOpened;
        }

        private void SelectTabByIndex(int tabIndex)
        {
            if (tabIndex < 0)
                return;

            var tabIndexString = tabIndex.ToString();
            Tabs.Controls.OfType<Tab>().ForEach(tab => tab.Active = tab.Header == tabIndexString);
            Tabs.Active = tabIndex - 1;
            var tabsHtml = HtmlUtil.RenderControl(Tabs);
            TabsPanel.InnerHtml = tabsHtml;

            SheerResponse.Eval($"spe.changeTab({tabIndex});");
        }

        private void UpdateTabInfo(Item scriptItem, int tabIndex)
        {
            var itemEditing = scriptItem != null;
            string path;
            if (itemEditing)
            {
                var fullPath = scriptItem.Paths.Path;
                // Strip the ScriptLibraryPath prefix for scripts inside the library,
                // otherwise fall back to the full path (e.g. scripts opened from
                // a search gallery that live outside the library root).
                path = fullPath.StartsWith(ApplicationSettings.ScriptLibraryPath, StringComparison.OrdinalIgnoreCase)
                    ? fullPath.Substring(ApplicationSettings.ScriptLibraryPath.Length)
                    : fullPath;
            }
            else
            {
                path = String.Empty;
            }

            var title = itemEditing ? scriptItem.Name : $"Untitled{TabSequencer}";
            var icon = itemEditing ? scriptItem[FieldIDs.Icon] : "powershell/16x16/ise8.png";
            path = itemEditing ? path : title;
            var id = itemEditing ? scriptItem.ID.ToString() : string.Empty;
            var db = itemEditing ? scriptItem.Database.Name : string.Empty;

            var builder = new ImageBuilder
            {
                Src = Images.GetThemedImageSource(icon, ImageDimension.id16x16),
                Width = 16,
                Height = 16,
                Margin = "0px 8px 0px 0px",
                Align = "middle"
            };
            var startbarHtml = $"{builder}{title} - ISE";

            SheerResponse.Eval($"spe.changeTabDetails({tabIndex},'{path}', '{startbarHtml}', '{id}', '{db}');");
            SelectTabByIndex(tabIndex);
        }


        [HandleMessage("ise:run", true)]
        protected virtual void ClientExecute(ClientPipelineArgs args)
        {
            args.Parameters["message"] = "ise:run";
            if (!RequestSessionElevationEx(args, ApplicationNames.ISE, SessionElevationManager.ExecuteAction))
            {
                return;
            }

            PowerShellLog.Audit($"[ISE] action=scriptExecuting user={Context.User?.Name}");

            using (var scriptSession = ScriptSessionManager.NewSession(ApplicationNames.ISE, true))
            {
                var settings = scriptSession.Settings;
                try
                {
                    if (UseContext)
                    {
                        scriptSession.SetItemLocationContext(ContextItem);
                    }

                    scriptSession.SetExecutedScript(ScriptItem);
                    scriptSession.ExecuteScriptPart(Editor.Value);
                    if (scriptSession.Output != null)
                    {
                        var clientOutputBuffer = new StringBuilder();
                        if (scriptSession.Output.GetConsoleUpdate(clientOutputBuffer, JstermBufferSize))
                        {
                            PrintSessionUpdate(clientOutputBuffer.ToString());
                        }
                    }
                }
                catch (Exception exc)
                {
                    var error = ScriptSession.GetExceptionString(exc, ScriptSession.ExceptionStringFormat.Console);
                    PrintSessionUpdate(error);
                }

                if (settings.SaveLastScript)
                {
                    settings.Load();
                    settings.LastScript = Editor.Value;
                    settings.Save();
                }

                TrySaveActiveTabs();
            }
        }

        [HandleMessage("ise:execute", true)]
        protected virtual void JobExecute(ClientPipelineArgs args)
        {
            args.Parameters.Add("message", "ise:execute");
            JobExecuteScript(args, Editor.Value, false);
        }

        /// <summary>
        ///     Terminal command execution entry point. The client writes the typed
        ///     command into the TerminalCommand hidden Memo and posts this message.
        ///     Routing through JobExecuteScript means the terminal shares the
        ///     editor's full execution pipeline: Monitor-driven job message queue,
        ///     ScriptRunning flag, ribbon state, progress overlay, Abort, and the
        ///     PromptForChoice / modal dialog machinery that ScriptingHostUserInterface
        ///     relies on. The web service ExecuteCommand path is no longer used by
        ///     the ISE (it remains for the standalone SPE Console).
        /// </summary>
        [HandleMessage("ise:termexecute", true)]
        protected virtual void JobExecuteTerm(ClientPipelineArgs args)
        {
            args.Parameters.Add("message", "ise:termexecute");
            var command = TerminalCommand?.Value ?? string.Empty;
            // Clear the memo so subsequent postbacks don't pick up stale content.
            if (TerminalCommand != null)
            {
                TerminalCommand.Value = string.Empty;
            }
            JobExecuteScript(args, command, false);
        }

        [HandleMessage("ise:debug", true)]
        protected virtual void Debug(ClientPipelineArgs args)
        {
            args.Parameters.Add("message", "ise:debug");
            JobExecuteScript(args, Editor.Value, true);
        }

        [HandleMessage("ise:executeselection", true)]
        protected virtual void JobExecuteSelection(ClientPipelineArgs args)
        {
            args.Parameters.Add("message", "ise:executeselection");
            JobExecuteScript(args, SelectionText.Value, false);
        }

        private static bool TryValidateAgainstPolicy(string script)
        {
            if (string.IsNullOrEmpty(CurrentPolicy)) return true;

            var policyItem = ResolveCurrentPolicyItem();
            if (policyItem == null)
            {
                PrintSessionUpdate("[[;#FF9494;]The selected policy item could not be resolved. Clear the selection and pick another.]");
                return false;
            }

            var policy = RemotingPolicyManager.GetPolicyFromItem(policyItem);
            if (policy == null)
            {
                PrintSessionUpdate($"[[;#FF9494;]Policy '{policyItem.Name}' could not be parsed.]");
                return false;
            }

            if (!ScriptValidator.ValidateScriptAgainstPolicy(policy, script, Context.User?.Name, "ISE", out var blocked))
            {
                PrintSessionUpdate($"[[;#FF9494;]Blocked by policy '{policy.Name}': command '{blocked}' is not in AllowedCommands.]");
                return false;
            }

            return true;
        }

        private static System.Management.Automation.PSLanguageMode ResolveLanguageMode(bool debug)
        {
            // Debug bypasses policy constraints so breakpoints and the debugger
            // protocol work; without this, Set-PSBreakpoint is blocked in CLM.
            if (debug) return System.Management.Automation.PSLanguageMode.FullLanguage;

            var policy = RemotingPolicyManager.GetPolicyFromItem(ResolveCurrentPolicyItem());
            if (policy == null) return System.Management.Automation.PSLanguageMode.FullLanguage;

            return policy.FullLanguage
                ? System.Management.Automation.PSLanguageMode.FullLanguage
                : System.Management.Automation.PSLanguageMode.ConstrainedLanguage;
        }

        protected virtual void JobExecuteScript(ClientPipelineArgs args, string scriptToExecute, bool debug)
        {
            if (!debug && !TryValidateAgainstPolicy(scriptToExecute))
            {
                return;
            }

            if (!RequestSessionElevationEx(args, ApplicationNames.ISE, SessionElevationManager.ExecuteAction))
            {
                return;
            }

            Debugging = debug;
            var sessionName = CurrentSessionId;
            if (string.Equals(sessionName, StringTokens.PersistentSessionId, StringComparison.OrdinalIgnoreCase))
            {
                var script = ScriptItem;
                sessionName = script != null ? script[Templates.Script.Fields.PersistentSessionId] : string.Empty;
            }

            var autoDispose = string.IsNullOrEmpty(sessionName);
            var scriptSession = autoDispose
                ? ScriptSessionManager.NewSession(ApplicationNames.ISE, true)
                : ScriptSessionManager.GetSession(sessionName, ApplicationNames.ISE, true);

            if (scriptSession.State == RunspaceAvailability.AvailableForNestedCommand || scriptSession.State == RunspaceAvailability.Busy)
            { 
                var errorMessage =
                    "A Script is already executing in this script session. Use another session or wait for the other script to finish.";
                PrintSessionUpdate($"[[;#FF9494;]{errorMessage}]");
                SheerResponse.Eval(
                    "spe.showSessionIDGallery();");
                return;
            }
            if (debug)
            {
                scriptSession.DebugFile = FileUtil.MapPath(Settings.TempFolderPath) + "\\" +
                                          Path.GetFileNameWithoutExtension(Path.GetTempFileName()) +
                                          ".ps1";
                File.WriteAllText(scriptSession.DebugFile, scriptToExecute);
                if (!string.IsNullOrEmpty(Breakpoints.Value))
                {
                    var strBrPoints = (Breakpoints.Value ?? string.Empty).Split(',');
                    var bPoints = strBrPoints.Select(int.Parse);
                    scriptSession.SetBreakpoints(bPoints);
                }

                scriptToExecute = scriptSession.DebugFile;
            }

            if (UseContext)
            {
                scriptSession.SetItemLocationContext(ContextItem);
            }

            scriptSession.Interactive = true;

            JobExecuteScript(args, scriptToExecute, scriptSession, autoDispose, debug);
        }

        protected virtual void JobExecuteScript(ClientPipelineArgs args, string scriptToExecute,
            ScriptSession scriptSession, bool autoDispose, bool debug)
        {
            ScriptRunning = true;
            UpdateRibbon();

            // Reset streaming trackers for the new execution. session.Output
            // is cleared by ScriptRunner's finally (ClearSilent), so we
            // start at zero lines and no pending partial.
            CommittedLineCount = 0;
            HasPendingPartial = false;

            PowerShellLog.Audit($"[ISE] action=scriptExecuting user={Context.User?.Name}");

            scriptSession.SetExecutedScript(ScriptItem);

            var progressBoxRunner = new ScriptRunner(ExecuteInternal, scriptSession, scriptToExecute, autoDispose);

            var rnd = new Random();
            var randomIndex = rnd.Next(ExecutionMessages.PleaseWaitMessages.Length - 1);
            var executionMessage = ExecutionMessages.PleaseWaitMessages[randomIndex];
            // Show the inline busy indicator in the terminal prompt line. The
            // client animates a spinner next to the fun message until the
            // script ends (spe.hideBusy is called from spe.scriptExecutionEnded).
            var encodedBusyMessage = HttpUtility.JavaScriptStringEncode(executionMessage, true);
            Context.ClientPage.ClientResponse.Eval(
                $"if(spe.showBusy){{spe.showBusy({encodedBusyMessage});}}");

            Context.ClientPage.ClientResponse.Eval(
                "if(spe.preventCloseWhenRunning){spe.preventCloseWhenRunning(true);}");

            scriptSession.Debugging = debug;
            scriptSession.SetLanguageMode(ResolveLanguageMode(debug));
            Monitor.Start($"{DefaultSessionName}", "ISE", progressBoxRunner.Run,
                LanguageManager.IsValidLanguageName(CurrentLanguage)
                    ? LanguageManager.GetLanguage(CurrentLanguage)
                    : Context.Language,
                User.Exists(CurrentUser)
                    ? User.FromName(CurrentUser, true)
                    : Context.User);

            Monitor.SessionID = scriptSession.ID;
            SheerResponse.Eval("spe.restoreResults();");

            var settings = ApplicationSettings.GetInstance(ApplicationNames.ISE);
            if (settings.SaveLastScript)
            {
                settings.Load();
                settings.LastScript = Editor.Value;
                settings.Save();
            }

            TrySaveActiveTabs();
        }

        [HandleMessage("ise:runplugin", true)]
        protected void RunPlugin(ClientPipelineArgs args)
        {
            var scriptSession = ScriptSessionManager.NewSession(ApplicationNames.ISE, true);
            var scriptDb = args.Parameters["scriptDb"];
            var scriptItem = args.Parameters["scriptId"];
            var script = Factory.GetDatabase(scriptDb).GetItem(scriptItem);
            if (!script.IsPowerShellScript()) return;

            scriptSession.SetVariable("scriptText", Editor.Value);
            scriptSession.SetVariable("selectionText", SelectionText.Value.Trim());
            scriptSession.SetVariable("scriptItem", ScriptItem);
            scriptSession.Interactive = true;
            JobExecuteScript(args, script[Templates.Script.Fields.ScriptBody], scriptSession, true, false);
        }

        [HandleMessage("ise:pluginupdate", true)]
        protected void ConsumePluginResult(ClientPipelineArgs args)
        {
            var script = args.Parameters["script"];
            if (!string.IsNullOrEmpty(script))
            {
                Editor.Value = script;
            }

            SheerResponse.Eval("spe.updateEditor();");
        }

        [HandleMessage("ise:plugininsert", true)]
        protected void ConsumePluginResultInsert(ClientPipelineArgs args)
        {
            var script = args.Parameters["script"];
            if (string.IsNullOrEmpty(script)) return;

            script = HttpUtility.JavaScriptStringEncode(script);
            SheerResponse.Eval($"spe.insertEditorContent(\"{script}\");");
        }

        [HandleMessage("pstaskmonitor:check", true)]
        protected void PrintOutput(ClientPipelineArgs args)
        {
            if (!(ScriptSessionManager.GetSessionIfExists(Monitor.SessionID) is ScriptSession session)) return;

            // If the script called Clear-Host, purge the browser's terminal
            // output before appending any new output from this poll cycle.
            if (session.Output.ConsumeClearPending())
            {
                ClearOutput();
                CommittedLineCount = 0;
                HasPendingPartial = false;
            }

            StreamOutputToTerminal(session);
        }

        // Max per-update buffer size for jsterm output, matching what the
        // standalone SPE Console web service passes (128 KiB).
        private const int JstermBufferSize = 131072;

        /// <summary>
        ///     Streams pending output to the ISE terminal in jquery.terminal
        ///     native format (jsterm), supporting inline-append for
        ///     Write-Host -NoNewline. Iterates session.Output by index,
        ///     groups lines into batches (up to a terminator or the end of
        ///     the buffer), and emits:
        ///       - spe.appendOutput(jsterm)          for terminated batches
        ///         (new committed line)
        ///       - spe.commitPartialOutput(jsterm)   when a previously
        ///         pending partial is now frozen by a terminator
        ///       - spe.updatePartialOutput(jsterm)   for the unterminated
        ///         tail (the line may still grow on subsequent polls)
        ///
        ///     After the drain, calls session.Output.GetHtmlUpdate() to
        ///     advance the OutputBuffer.updatePointer so that ScriptRunner's
        ///     later GetConsoleUpdate drain does not re-emit the same lines.
        /// </summary>
        private void StreamOutputToTerminal(ScriptSession session)
        {
            var output = session.Output;
            var committed = CommittedLineCount;
            var hasPending = HasPendingPartial;

            var totalLines = output.Count;

            // The buffer was cleared since the last poll (e.g. by Clear-Host
            // or a session reset). Reset our tracking.
            if (totalLines < committed)
            {
                committed = 0;
                hasPending = false;
            }

            // Find the boundary between terminated and unterminated content.
            int lastTerminated = -1;
            for (int i = totalLines - 1; i >= committed; i--)
            {
                if (output[i].Terminated)
                {
                    lastTerminated = i;
                    break;
                }
            }

            // Emit all terminated content as a single appendOutput (or
            // commitPartialOutput if there was a pending partial). One
            // echo() call with the full concatenated jsterm string.
            if (lastTerminated >= committed)
            {
                var sb = new StringBuilder();
                for (int i = committed; i <= lastTerminated; i++)
                {
                    output[i].GetLine(sb, OutputLine.FormatResponseJsterm);
                }
                var encoded = HttpUtility.JavaScriptStringEncode(sb.ToString(), true);
                if (hasPending)
                {
                    SheerResponse.Eval($"spe.commitPartialOutput({encoded});");
                    hasPending = false;
                }
                else
                {
                    SheerResponse.Eval($"spe.appendOutput({encoded});");
                }
                committed = lastTerminated + 1;
            }

            // If there is an unterminated tail, emit it as a partial.
            if (committed < totalLines)
            {
                var sb = new StringBuilder();
                for (int i = committed; i < totalLines; i++)
                {
                    output[i].GetLine(sb, OutputLine.FormatResponseJsterm);
                }
                var encoded = HttpUtility.JavaScriptStringEncode(sb.ToString(), true);
                SheerResponse.Eval($"spe.updatePartialOutput({encoded});");
                hasPending = true;
            }

            // Sync the OutputBuffer's internal updatePointer with what we
            // have rendered so the ScriptRunner's end-of-run drain (which
            // uses GetConsoleUpdate) does not re-emit the same lines. The
            // returned HTML is discarded - we only care about the side
            // effect of advancing the pointer.
            var _ = output.GetHtmlUpdate();

            CommittedLineCount = committed;
            HasPendingPartial = hasPending;
        }

        private static void ClearOutput()
        {
            SheerResponse.Eval($"spe.clearOutput();");
        }

        /// <summary>
        ///     Emits a jquery.terminal format string (jsterm) to the ISE
        ///     terminal as a committed (non-partial) line.
        /// </summary>
        private static void PrintSessionUpdate(string jstermText)
        {
            if (string.IsNullOrEmpty(jstermText)) return;
            var encoded = HttpUtility.JavaScriptStringEncode(jstermText, true);
            SheerResponse.Eval($"spe.appendOutput({encoded});");
        }

        /// <summary>
        ///     Emits a raw HTML fragment to the ISE terminal via the raw-echo
        ///     path. Used for the small set of UI affordances the ISE renders
        ///     with bespoke styling: error spans, session-busy warnings, and
        ///     the deferred-action blocks produced by ScriptExecutionResult.
        /// </summary>
        private static void PrintHtmlSessionUpdate(string html)
        {
            if (string.IsNullOrEmpty(html)) return;

            var xssCleanup =
                new Regex(@"<script[^>]*>[\s\S]*?</script>|<noscript[^>]*>[\s\S]*?</noscript>|<img.*onerror.*>");
            if (xssCleanup.IsMatch(html))
            {
                html = xssCleanup.Replace(html, "<div title='Script tag removed'>&#9888;</div>");
            }

            html = HttpUtility.HtmlEncode(html.Replace("\r", "").Replace("\n", "<br/>")).Replace("\\", "&#92;");
            SheerResponse.Eval($"spe.appendHtmlOutput(\"{html}\");");
        }

        protected void ExecuteInternal(ScriptSession scriptSession, string script)
        {
            try
            {
                if (scriptSession.Debugging)
                {
                    scriptSession.InitBreakpoints();
                }

                scriptSession.ExecuteScriptPart(script);
            }
            finally
            {
                scriptSession.SetLanguageMode(System.Management.Automation.PSLanguageMode.FullLanguage);

                if (!string.IsNullOrEmpty(scriptSession.DebugFile))
                {
                    File.Delete(scriptSession.DebugFile);
                    scriptSession.DebugFile = string.Empty;
                    scriptSession.ExecuteScriptPart("Get-PSBreakpoint | Remove-PSBreakpoint");
                }

                scriptSession.Debugging = false;
                scriptSession.Interactive = false;
            }
        }

        [HandleMessage("ise:abort", true)]
        protected virtual void JobAbort(ClientPipelineArgs args)
        {
            if (ScriptSessionManager.GetSessionIfExists(Monitor.SessionID) is ScriptSession currentSession)
            {
                currentSession.Abort();

                if (currentSession.AutoDispose)
                {
                    currentSession.Dispose();
                }
            }
            else
            {
                ScriptRunning = false;
                UpdateRibbon();
            }

            Monitor.SessionID = string.Empty;
            ScriptRunning = false;
        }

        private void MonitorOnJobFinished(object sender, EventArgs eventArgs)
        {
            var args = eventArgs as SessionCompleteEventArgs;
            var result = args?.RunnerOutput;

            // If the polling drain left a pending partial on the client,
            // finalize it before emitting anything else - the content the
            // client has rendered is the final text of that partial, and
            // subsequent appendOutput calls must start a fresh visual line.
            if (HasPendingPartial)
            {
                SheerResponse.Eval("spe.finalizePartial();");
                HasPendingPartial = false;
            }

            if (result != null)
            {
                if (result.ClearHost)
                {
                    ClearOutput();
                }
                PrintSessionUpdate(result.Output);
            }

            if (result?.Exception != null)
            {
                var error = ScriptSession.GetExceptionString(result.Exception,
                    ScriptSession.ExceptionStringFormat.Console);
                PrintSessionUpdate(error);
            }

            var executionResult = new ScriptExecutionResult(result);
            // GetIseResult yields literal HTML fragments with bespoke CSS
            // classes (.deferred, .label, .content) - those go through the
            // HTML path.
            executionResult.GetIseResult(result.CloseRunner).ForEach(PrintHtmlSessionUpdate);

            ProgressOverlay.Visible = false;
            ScriptRunning = false;

            // Push the updated prompt before signalling the client that the
            // script has ended - the session's location may have changed during
            // execution (cd, Set-Location, Push-Location), and we want the
            // terminal's prompt to reflect the new location the instant the
            // command line becomes usable again.
            var finishedSession = ScriptSessionManager.GetSessionIfExists(Monitor.SessionID);
            if (finishedSession != null)
            {
                PushTerminalPrompt(finishedSession);
            }

            Monitor.SessionID = string.Empty;
            UpdateRibbon();
            SheerResponse.Eval("spe.scriptExecutionEnded()");
        }

        [HandleMessage("ise:updateprogress", true)]
        protected virtual void UpdateProgress(ClientPipelineArgs args)
        {
            var showProgress = ScriptRunning &&
                               !string.Equals(args.Parameters["RecordType"], "Completed",
                                   StringComparison.OrdinalIgnoreCase);
            ProgressOverlay.Visible = showProgress;
            var sb = new StringBuilder();
            if (showProgress)
            {
                sb.AppendFormat("<h2>{0}</h2>", args.Parameters["Activity"]);
                if (!string.IsNullOrEmpty(args.Parameters["StatusDescription"]))
                {
                    sb.AppendFormat("<p>{0}</p>", args.Parameters["StatusDescription"]);
                }

                if (!string.IsNullOrEmpty(args.Parameters["PercentComplete"]))
                {
                    var percentComplete = int.Parse(args.Parameters["PercentComplete"]);
                    if (percentComplete > -1)
                        sb.AppendFormat("<div id='progressbar'><div style='width:{0}%'></div></div>", percentComplete);
                }

                if (!string.IsNullOrEmpty(args.Parameters["SecondsRemaining"]))
                {
                    var secondsRemaining = int.Parse(args.Parameters["SecondsRemaining"]);
                    if (secondsRemaining > -1)
                        sb.AppendFormat("<p><strong>{0:c} </strong> " +
                                        Texts.PowerShellIse_UpdateProgress_remaining +
                                        ".</p>",
                            new TimeSpan(0, 0, secondsRemaining));
                }

                if (!string.IsNullOrEmpty(args.Parameters["CurrentOperation"]))
                {
                    sb.AppendFormat("<p>{0}</p>", args.Parameters["CurrentOperation"]);
                }
            }

            Progress.Text = sb.ToString();
        }

        [HandleMessage("ise:scriptchanged")]
        protected void NotifyScriptModified(Message message)
        {
            bool.TryParse(message.Arguments["modified"], out var modified);
            ScriptModified = modified;
            UpdateRibbon();
        }

        [HandleMessage("ise:closetab")]
        protected void CloseTabInitiated(Message message)
        {
            bool.TryParse(message.Arguments["modified"], out var modified);
            if (!int.TryParse(message.Arguments["index"], out var tabIndex))
                tabIndex = -1;

            if (!int.TryParse(message.Arguments["selectIndex"], out var previouslySelectedIndex))
                previouslySelectedIndex = -1;

            if (modified)
            {
                var parameters = new NameValueCollection
                {
                    ["index"] = $"{tabIndex}",
                    ["message"] =
                        Texts.PowerShellIse_The_script_is_modified_Do_you_want_to_close_it_without_saving_the_changes,
                    ["selectIndex"] = $"{previouslySelectedIndex}"
                };
                Context.ClientPage.Start(this, "CloseModifiedScript", parameters);
            }
            else
            {
                CloseTab(tabIndex, previouslySelectedIndex);
            }

            UpdateRibbon();
        }

        protected void CloseModifiedScript(ClientPipelineArgs args)
        {
            if (args.IsPostBack)
            {
                if (args.Result == "yes")
                {
                    if (!int.TryParse(args.Parameters["index"], out int index))
                    {
                        index = -1;
                    }

                    if (!int.TryParse(args.Parameters["selectIndex"], out int selectIndex))
                        selectIndex = -1;

                    CloseTab(index, selectIndex);
                }
            }
            else
            {
                var index = int.Parse(args.Parameters["index"]);
                SelectTabByIndex(index);
                SheerResponse.Confirm(args.Parameters["message"]);
                args.WaitForPostBack();
            }
        }

        private void CloseTab(int index, int newSelectedIndex)
        {
            if (index < 0)
                return;
            var tab = Tabs.Controls.OfType<Tab>().Skip(Tabs.Active).FirstOrDefault();
            if (tab != null)
            {
                Tabs.Controls.RemoveAt(Tabs.Controls.Count - 1);
            }

            SheerResponse.Eval($"spe.closeScript({index});");
            if (Tabs.Controls.Count == 0)
            {
                CreateNewTab(null);
            }
            else
            {
                SelectTabByIndex(newSelectedIndex);
            }
            TrySaveActiveTabs();
        }

        [HandleMessage("ise:updateribbon")]
        protected void UpdateRibbon(Message message)
        {
            UpdateRibbon();
        }

        /// <summary>
        ///     Updates the ribbon.
        /// </summary>
        private void UpdateRibbon()
        {
            var ribbon = new Ribbon { ID = "PowerShellRibbon" };
            var item = ScriptItem;
            ribbon.CommandContext = new CommandContext(item);
            ribbon.ShowContextualTabs = false;
            ribbon.CommandContext.Parameters["ScriptRunning"] = ScriptRunning ? "1" : "0";
            ribbon.CommandContext.Parameters["currentSessionId"] = CurrentSessionId ?? string.Empty;
            ribbon.CommandContext.Parameters["scriptLength"] = Editor.Value.Length.ToString();
            ribbon.CommandContext.Parameters["selectionLength"] = SelectionText.Value.Trim().Length.ToString();
            ribbon.CommandContext.Parameters["modified"] = ScriptModified.ToString();
            ribbon.CommandContext.Parameters["debugging"] = Debugging ? "1" : "0";
            ribbon.CommandContext.Parameters["inBreakpoint"] = InBreakpoint ? "1" : "0";


            var sessionName = CurrentSessionId ?? string.Empty;
            var persistentSessionId = sessionName;
            if (string.Equals(sessionName, StringTokens.PersistentSessionId, StringComparison.OrdinalIgnoreCase))
            {
                var name =
                    item != null &&
                    !item[Templates.Script.Fields.PersistentSessionId].IsNullOrEmpty()
                        ? item[Templates.Script.Fields.PersistentSessionId]
                        : null;
                sessionName = string.Format(Texts.PowerShellIse_UpdateRibbon_Script_defined___0_,
                    name ?? Texts.PowerShellIse_UpdateRibbon_Single_execution);
                persistentSessionId = name ?? string.Empty;
            }

            ribbon.CommandContext.Parameters["persistentSessionId"] = persistentSessionId;
            ribbon.CommandContext.Parameters["currentSessionName"] = string.IsNullOrEmpty(sessionName)
                ? Texts.PowerShellIse_UpdateRibbon_Single_execution
                : (sessionName == DefaultSessionName)
                    ? Factory.GetDatabase("core")
                        .GetItem(
                            "/sitecore/content/Applications/PowerShell/PowerShellIse/Menus/Sessions/ISE editing session")
                        ?
                        .DisplayName ?? DefaultSessionName
                    : sessionName;
            var obj2 = Context.Database.GetItem("/sitecore/content/Applications/PowerShell/PowerShellIse/Ribbon");
            Error.AssertItemFound(obj2, "/sitecore/content/Applications/PowerShell/PowerShellIse/Ribbon");
            ribbon.CommandContext.RibbonSourceUri = obj2.Uri;

            ribbon.CommandContext.Parameters["currentUser"] = string.IsNullOrEmpty(CurrentUser)
                ? DefaultUser
                : CurrentUser;
            ribbon.CommandContext.Parameters["currentLanguage"] = string.IsNullOrEmpty(CurrentLanguage)
                ? DefaultLanguage
                : CurrentLanguage;
            ribbon.CommandContext.Parameters["currentPolicy"] = CurrentPolicy ?? string.Empty;

            ribbon.CommandContext.Parameters.Add("contextDB", UseContext ? ContextItemDb : string.Empty);
            ribbon.CommandContext.Parameters.Add("contextItem", UseContext ? ContextItemId : string.Empty);
            ribbon.CommandContext.Parameters.Add("scriptDB", ScriptItemDb);
            ribbon.CommandContext.Parameters.Add("scriptItem", ScriptItemId);
            RibbonPanel.InnerHtml = HtmlUtil.RenderControl(ribbon);

            UpdateWarning();
        }

        private void UpdateWarning(string updateFromMessage = "")
        {
            var isSessionElevated = SessionElevationManager.IsSessionTokenElevated(ApplicationNames.ISE);

            var controlContent = string.Empty;
            var hidePanel = false;
            var tokenAction = SessionElevationManager.GetToken(ApplicationNames.ISE).Action;
            switch (tokenAction)
            {
                case SessionElevationManager.TokenDefinition.ElevationAction.Allow:
                    // it is always elevated
                    hidePanel = true;
                    break;
                case SessionElevationManager.TokenDefinition.ElevationAction.Password:
                case SessionElevationManager.TokenDefinition.ElevationAction.Confirm:
                    // show that session elevation can be dropped
                    if (isSessionElevated)
                    {
                        controlContent = HtmlUtil.RenderControl(ElevatedPanel);
                    }
                    else
                    {
                        if (WasElevated)
                        {
                            // we're cool devs know that session will need to be elevated.
                            hidePanel = true;
                        }
                        else
                        {
                            controlContent = HtmlUtil.RenderControl(ElevationRequiredPanel);
                        }
                    }

                    break;
                case SessionElevationManager.TokenDefinition.ElevationAction.Block:
                    controlContent = HtmlUtil.RenderControl(ElevationBlockedPanel);
                    break;
            }

            InfoPanel.InnerHtml = controlContent;
            InfoPanel.CssClass = hidePanel ? "scEditorWarningHidden" : "scEditorWarning";
            SheerResponse.Eval($"spe.showInfoPanel({(!hidePanel).ToString().ToLower()}, '{updateFromMessage}');");
        }

        [HandleMessage("item:updated", true)]
        protected void FieldEditor(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
        }

        [HandleMessage("ise:setsessionid", true)]
        protected void SetCurrentSessionId(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            var sessionId = args.Parameters["id"];
            if (IsHackedParameter(sessionId))
            {
                return;
            }

            CurrentSessionId = sessionId;
            SheerResponse.Eval($"spe.changeSessionId('{sessionId}');");
            UpdateRibbon();
        }

        [HandleMessage("ise:setlanguage", true)]
        protected void SetCurrentLanguage(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            var language = args.Parameters["language"];
            if (IsHackedParameter(language))
            {
                return;
            }

            CurrentLanguage = language;
            new LanguageHistory().Add(language);
            UpdateRibbon();
        }

        [HandleMessage("ise:setuser", true)]
        protected void SetCurrentUser(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            var user = args.Parameters["user"];
            if (IsHackedParameter(user))
            {
                return;
            }

            CurrentUser = user;
            new UserHistory().Add(user);
            UpdateRibbon();
        }

        [HandleMessage("ise:setpolicy", true)]
        protected void SetPolicy(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            var idStr = args.Parameters["id"] ?? string.Empty;
            if (IsHackedParameter(idStr))
            {
                return;
            }

            if (string.IsNullOrEmpty(idStr))
            {
                CurrentPolicy = string.Empty;
                UpdateRibbon();
                return;
            }

            var item = RemotingPolicyManager.ResolvePolicyItem(idStr);
            if (item == null)
            {
                SheerResponse.Alert("The selected policy item no longer exists.");
                return;
            }

            // Drop the policy cache so a just-created or just-renamed policy is
            // visible on the next remoting validation without waiting for the TTL.
            RemotingPolicyManager.Invalidate();

            CurrentPolicy = item.ID.ToString();
            UpdateRibbon();
        }

        // Script IDs for the Content-Editor-shipped policy management scripts.
        // Keeping the same script as a single source of truth so Content Editor and
        // ISE show the exact same Edit / Create UI.
        private static readonly ID EditPolicyScriptId = new ID("{4E7E1CD8-BD37-448A-AA12-46A2F29869EE}");
        private static readonly ID CreatePolicyScriptId = new ID("{DDE4EAA6-5B99-48EC-9DD4-BB4A8CEAE443}");

        [HandleMessage("ise:createpolicy", true)]
        protected void CreatePolicy(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            var db = Factory.GetDatabase(ApplicationSettings.ScriptLibraryDb);
            var policiesFolder = db?.GetItem(ItemIDs.Policies);
            if (policiesFolder == null)
            {
                SheerResponse.Alert("Remoting Policies folder was not found.");
                return;
            }

            LaunchPolicyEditor(CreatePolicyScriptId, policiesFolder);
        }

        [HandleMessage("ise:editpolicy", true)]
        protected void EditPolicy(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            if (string.IsNullOrEmpty(CurrentPolicy))
            {
                SheerResponse.Alert("Select a policy from the Policy dropdown first.");
                return;
            }

            var policyItem = ResolveCurrentPolicyItem();
            if (policyItem == null)
            {
                SheerResponse.Alert("The selected policy item could not be resolved. It may have been deleted.");
                return;
            }

            LaunchPolicyEditor(EditPolicyScriptId, policyItem);
        }

        private void LaunchPolicyEditor(ID scriptId, Item contextItem)
        {
            if (!AuxiliaryMonitor.JobHandle.Equals((object)Handle.Null))
            {
                SheerResponse.Alert("A policy editor is already open.");
                return;
            }

            var scriptItem = Factory.GetDatabase(ApplicationSettings.ScriptLibraryDb)?.GetItem(scriptId);
            if (scriptItem == null || !scriptItem.IsPowerShellScript())
            {
                SheerResponse.Alert("Policy management script not found in the script library.");
                return;
            }

            // Throwaway session, not named and autoDispose=true. ScriptRunner.Run
            // disposes it in its finally block, so we don't need to track it.
            // Output, progress and any terminal-bound messages go to this session's
            // buffer and are never streamed to the ISE terminal because the main
            // Monitor.SessionID stays tied to the ISE's display session.
            var session = ScriptSessionManager.NewSession(ApplicationNames.ISE, true);
            session.SetExecutedScript(scriptItem);
            session.SetItemLocationContext(contextItem);
            session.Interactive = true;

            var runner = new ScriptRunner(ExecuteInternal, session,
                scriptItem[Templates.Script.Fields.ScriptBody], true);

            // Starting on AuxiliaryMonitor keeps the job's dialog messages flowing
            // through a monitor that is not wired to MonitorOnJobFinished,
            // so the ISE terminal, ribbon state and prompt are not disturbed
            // when the script opens its Invoke-Dialog modal and later ends.
            AuxiliaryMonitor.Start($"Edit policy \"{contextItem.Name}\"", "ISE-Auxiliary",
                runner.Run, Context.Language, Context.User);
        }

        private void AuxiliaryMonitorOnJobFinished(object sender, EventArgs e)
        {
            // Currently the only auxiliary consumer is the Remoting Policy
            // editor/creator, so drop the policy cache here. If other side
            // jobs are added later this should be split into per-job handlers
            // (attach before Start, detach inside the callback).
            RemotingPolicyManager.Invalidate();
        }

        [HandleMessage("ise:setcontextitem", true)]
        protected void SetContextItem(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            var contextDb = args.Parameters["db"];
            var contextId = args.Parameters["id"];
            if (string.IsNullOrEmpty(contextId) || string.IsNullOrEmpty(contextDb))
            {
                UseContext = false;
                UpdateRibbon();
                return;
            }

            UseContext = true;
            if (Factory.GetDatabase(contextDb).GetItem(contextId) != null)
            {
                ContextItemDb = contextDb;
                ContextItemId = contextId;
            }

            UpdateRibbon();
            // Reprime the terminal session with the new context item and refresh its prompt
            PrimeTerminalSession();
        }

        [HandleMessage("ise:initterminal", true)]
        protected void InitTerminalSession(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            PrimeTerminalSession();
        }

        /// <summary>
        /// Creates or retrieves the ISE PowerShell session and primes it with the
        /// ISE's context (current item location, interactive flag). Then notifies
        /// the client terminal so it can fetch the updated prompt.
        /// </summary>
        private void PrimeTerminalSession()
        {
            try
            {
                var session = ScriptSessionManager.GetSession(CurrentSessionId, ApplicationNames.ISE, true);
                session.Interactive = true;
                if (UseContext && ContextItem != null)
                {
                    session.SetItemLocationContext(ContextItem);
                }
                // Push the current prompt to the client terminal directly - no
                // client-initiated round trip needed since we already have the
                // session here.
                PushTerminalPrompt(session);
            }
            catch (Exception ex)
            {
                PowerShellLog.Error($"[ISE] action=primeTerminalSession failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        ///     Pushes the current PS prompt for the given session to the client
        ///     terminal. Called after session priming and after script execution
        ///     completes so the terminal prompt reflects the session's current
        ///     location without the client having to make a web service round trip.
        /// </summary>
        private static void PushTerminalPrompt(ScriptSession session)
        {
            if (session == null) return;
            var prompt = $"PS {session.CurrentLocation}>";
            var encoded = HttpUtility.JavaScriptStringEncode(prompt, true);
            SheerResponse.Eval($"if (spe.setTerminalPrompt) {{ spe.setTerminalPrompt({encoded}); }}");
        }

        [HandleMessage("ise:updatesettings", true)]
        protected virtual void UpdateSettings(ClientPipelineArgs args)
        {
            var settings = ApplicationSettings.GetInstance(ApplicationNames.ISE);
            var backgroundColor = OutputLine.ProcessHtmlColor(settings.BackgroundColor);
            var bottomPadding = 0;
            SheerResponse.Eval(
                $"spe.changeSettings('{settings.FontFamilyStyle}', {settings.FontSize}, " +
                $"'{backgroundColor}', {bottomPadding}," +
                $" {settings.LiveAutocompletion.ToString().ToLower()}," +
                $" {settings.PerTabOutput.ToString().ToLower()});");

            var session = ScriptSessionManager.GetSession(CurrentSessionId, ApplicationNames.ISE, true);
            session.PrivateData.BackgroundColor = settings.BackgroundColor;
            session.PrivateData.ForegroundColor = settings.ForegroundColor;
        }

        [HandleMessage("ise:setbreakpoint", true)]
        protected virtual void SetBreakpoint(ClientPipelineArgs args)
        {
            var line = args.Parameters["Line"];
            var action = args.Parameters["Action"];
            SheerResponse.Eval($"$ise(function() {{{{ spe.breakpointSet({line}, '{action}'); }}}});");
        }

        [HandleMessage("ise:togglebreakpoint", true)]
        protected virtual void ToggleRuntimeBreakpoint(ClientPipelineArgs args)
        {
            var line = int.Parse(args.Parameters["Line"]) + 1;
            var state = args.Parameters["State"] == "true";
            if (!(ScriptSessionManager.GetSessionIfExists(Monitor.SessionID) is ScriptSession session)) return;

            var bPointScript = state
                ? $"Set-PSBreakpoint -Script {session.DebugFile} -Line {line}"
                : $"Get-PSBreakpoint -Script {session.DebugFile} | ? {{ $_.Line -eq {line}}} | Remove-PSBreakpoint";
            bPointScript += " | Out-Null";
            session.TryInvokeInRunningSession(bPointScript);
            InBreakpoint = false;
        }

        [HandleMessage("ise:breakpointhit", true)]
        protected virtual void BreakpointHit(ClientPipelineArgs args)
        {
            var line = args.Parameters["Line"];
            var column = args.Parameters["Column"];
            var endLine = args.Parameters["EndLine"];
            var endColumn = args.Parameters["EndColumn"];
            var jobId = args.Parameters["JobId"];
            InBreakpoint = true;
            UpdateRibbon();
            SheerResponse.Eval(
                $"$ise(function() {{ spe.breakpointHit({line}, {column}, {endLine}, {endColumn}, '{jobId}'); }});");
        }


        [HandleMessage("ise:debugstart", true)]
        protected virtual void DebuggingStart(ClientPipelineArgs args)
        {
            SheerResponse.Eval("$ise(function() {{ spe.debugStart(); }});");
        }

        [HandleMessage("ise:debugend", true)]
        protected virtual void DebuggingEnd(ClientPipelineArgs args)
        {
            SheerResponse.Eval("$ise(function() {{ spe.debugStop(); }});");
        }

        [HandleMessage("ise:debugaction", true)]
        protected virtual void BreakpointAction(ClientPipelineArgs args)
        {
            if (!(ScriptSessionManager.GetSessionIfExists(Monitor.SessionID) is ScriptSession session)) return;

            session.TryInvokeInRunningSession(args.Parameters["action"]);
            SheerResponse.Eval("$ise(function() { spe.breakpointHandled(); });");
            InBreakpoint = false;
        }

        public void SessionElevationPipeline(ClientPipelineArgs args)
        {
            if (!args.IsPostBack)
            {
                var url = new UrlString(UIUtil.GetUri("control:PowerShellSessionElevation"));
                url.Parameters["action"] = args.Parameters["action"];
                url.Parameters["app"] = args.Parameters["app"];
                TypeResolver.Resolve<ISessionElevationWindowLauncher>().ShowSessionElevationWindow(url);
                args.WaitForPostBack(true);
            }
            else
            {
                WasElevated = true;
                var message = args.Parameters["message"];
                if (!message.IsNullOrEmpty())
                {
                    message = args.Parameters["message"] + "(elevationResult=1)";
                }

                UpdateWarning(message);
            }
        }

        [HandleMessage("ise:requestelevation", true)]
        protected virtual void RequestSessionExecuteElevation(ClientPipelineArgs args)
        {
            RequestSessionElevationEx(args, ApplicationNames.ISE, SessionElevationManager.ExecuteAction);
        }

        private bool RequestSessionElevationEx(ClientPipelineArgs args, string appName, string action)
        {
            if (SessionElevationManager.IsSessionTokenElevated(appName)) return true;

            if (args.Parameters.AllKeys.Contains("elevationResult"))
            {
                SessionElevationErrors.OperationRequiresElevation();
                return false;
            }

            var pipelineArgs = new ClientPipelineArgs
            {
                Parameters = { ["message"] = args.Parameters["message"], ["app"] = appName, ["action"] = action }
            };
            Context.ClientPage.Start(this, nameof(SessionElevationPipeline), pipelineArgs);
            return false;
        }

        public void DropElevationButtonClick()
        {
            SessionElevationManager.DropSessionTokenElevation(ApplicationNames.ISE);
            UpdateRibbon();
        }
    }
}