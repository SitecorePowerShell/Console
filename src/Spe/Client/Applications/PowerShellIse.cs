﻿using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI.WebControls;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.IO;
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
        protected Literal Progress;
        protected Border ProgressOverlay;
        protected Border RibbonPanel;
        protected Border ScriptResult;
        protected Memo SelectionText;
        protected Memo Breakpoints;
        protected GridPanel ElevationRequiredPanel;
        protected GridPanel ElevatedPanel;
        protected GridPanel ElevationBlockedPanel;
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

        public SpeJobMonitor Monitor { get; private set; }

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
                PowerShellLog.Warn($"User {Context.User?.Name} attempt to access PowerShell ISE - denied.");
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

            Monitor.JobFinished += MonitorOnJobFinished;

            Tabs.OnChange += TabsOnChange;
            if (Context.ClientPage.IsEvent)
                return;
            
            var settings = ApplicationSettings.GetInstance(ApplicationNames.ISE);

            if (settings.SaveLastScript)
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
            var openedScript = OpenedScripts.Value.Split('\n')[Tabs.Active].Split(':')[0];

            if (openedScript.IndexOf("/") > 0)
            {
                var scriptItem = Sitecore.Client.ContentDatabase.GetItem(ApplicationSettings.ScriptLibraryPath+openedScript);

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
            var name = scriptItem.Name;
            var icon = scriptItem[FieldIDs.Icon];

            var mruMenu = ApplicationSettings.GetIseMruContainerItem();
            var mruItems = mruMenu.Children;
            if (mruItems.Count == 0 || !(mruItems[0]["Message"].Contains(id)))
            {
                var openedScript = mruItems.FirstOrDefault(mruItem => mruItem["Message"].Contains(id)) ??
                                   mruMenu.Add(Guid.NewGuid().ToString("n"),
                                       new TemplateID(Sitecore.TemplateIDs.MenuItem));
                openedScript.Edit(args =>
                {
                    openedScript["Message"] = $"ise:mruopen(id={id},db={db})";
                    openedScript["Icon"] = icon;
                    openedScript[FieldIDs.Icon] = icon;
                    openedScript["Display name"] = name;
                    openedScript[FieldIDs.DisplayName] = name;
                    openedScript[FieldIDs.Sortorder] = "0";
                    openedScript.Publishing.NeverPublish = true;
                });

                var sortOrder = 1;
                foreach (Item mruItem in mruItems)
                {
                    if (sortOrder > 9)
                    {
                        mruItem.Delete();
                        continue;
                    }

                    if (!(mruItem["Message"].Contains(id)))
                    {
                        var item = mruItem;
                        item.Edit(args =>
                        {
                            item[Sitecore.FieldIDs.Sortorder] = sortOrder.ToString("G");
                            item.Publishing.NeverPublish = true;
                            sortOrder++;
                        });
                    }
                }
            }
        }

        [HandleMessage("ise:new", true)]
        protected void NewScript(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            ScriptItemId = string.Empty;
            ScriptItemDb = string.Empty;
            Editor.Value = string.Empty;
            ScriptResult.Value = "<pre ID='ScriptResultCode'></pre>";
            CreateNewTab(null);
            UpdateRibbon();
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

        [HandleMessage("ise:reload", true)]
        protected void ReloadItem(ClientPipelineArgs args)
        {
            LoadItem(ScriptItemDb, ScriptItemId);
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
            var path = itemEditing
                ? scriptItem.Paths.Path.Substring(ApplicationSettings.ScriptLibraryPath.Length)
                : String.Empty;

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

            PowerShellLog.Info($"Arbitrary script execution in ISE by user: '{Context.User?.Name}'");

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
                    ClearOutput();
                    if (scriptSession.Output != null)
                    {
                        PrintSessionUpdate(scriptSession.Output.GetHtmlUpdate());
                    }
                }
                catch (Exception exc)
                {
                    var error = ScriptSession.GetExceptionString(exc, ScriptSession.ExceptionStringFormat.Html);
                    PrintSessionUpdate($"<pre style='background:red;'>{error}</pre>");
                }

                if (settings.SaveLastScript)
                {
                    settings.Load();
                    settings.LastScript = Editor.Value;
                    settings.Save();
                }
            }
        }

        [HandleMessage("ise:execute", true)]
        protected virtual void JobExecute(ClientPipelineArgs args)
        {
            args.Parameters.Add("message", "ise:execute");
            JobExecuteScript(args, Editor.Value, false);
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

        protected virtual void JobExecuteScript(ClientPipelineArgs args, string scriptToExecute, bool debug)
        {
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

            ClearOutput();
            if (scriptSession.State == RunspaceAvailability.AvailableForNestedCommand || scriptSession.State == RunspaceAvailability.Busy)
            { 
                var errorMessage =
                    "A Script is already executing in this script session. Use another session or wait for the other script to finish.";
                PrintSessionUpdate($"<span style='background:red; color:white'>{errorMessage}</span>");
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

            PowerShellLog.Info($"Arbitrary script execution in ISE by user: '{Context.User?.Name}'");

            scriptSession.SetExecutedScript(ScriptItem);

            var progressBoxRunner = new ScriptRunner(ExecuteInternal, scriptSession, scriptToExecute, autoDispose);

            var rnd = new Random();
            var randomIndex = rnd.Next(ExecutionMessages.PleaseWaitMessages.Length - 1);
            var executionMessage = ExecutionMessages.PleaseWaitMessages[randomIndex];
            Context.ClientPage.ClientResponse.SetInnerHtml(
                "ScriptResult",
                string.Format(
                    "<div id='PleaseWait'>" +
                    "<img src='../../../../../sitecore modules/PowerShell/Assets/working.gif' alt='" +
                    Texts.PowerShellIse_JobExecuteScript_Working +
                    "' />" +
                    "<div>" +
                    Texts.PowerShellIse_JobExecuteScript_Please_wait___0_ +
                    "</div>" +
                    "</div>" +
                    "<pre ID='ScriptResultCode'></pre>", executionMessage));

            Context.ClientPage.ClientResponse.Eval(
                "if(spe.preventCloseWhenRunning){spe.preventCloseWhenRunning(true);}");

            scriptSession.Debugging = debug;
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

            var result = session.Output.GetHtmlUpdate();
            PrintSessionUpdate(result);
        }

        private static void ClearOutput()
        {
            SheerResponse.Eval($"spe.clearOutput();");
        }

        private static void PrintSessionUpdate(string result)
        {
            if (!string.IsNullOrEmpty(result))
            {
                var xssCleanup =
                    new Regex(@"<script[^>]*>[\s\S]*?</script>|<noscript[^>]*>[\s\S]*?</noscript>|<img.*onerror.*>");
                if (xssCleanup.IsMatch(result))
                {
                    result = xssCleanup.Replace(result, "<div title='Script tag removed'>&#9888;</div>");
                }

                result = HttpUtility.HtmlEncode(result.Replace("\r", "").Replace("\n", "<br/>")).Replace("\\", "&#92;");
                SheerResponse.Eval($"spe.appendOutput(\"{result}\");");
            }
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
            if (result != null)
            {
                PrintSessionUpdate(result.Output);
            }

            if (result?.Exception != null)
            {
                var error = ScriptSession.GetExceptionString(result.Exception,
                    ScriptSession.ExceptionStringFormat.Html);
                PrintSessionUpdate($"<pre style='background:red;'>{error}</pre>");
            }

            var executionResult = new ScriptExecutionResult(result);
            executionResult.GetIseResult(result.CloseRunner).ForEach(PrintSessionUpdate);
            
            SheerResponse.SetInnerHtml("PleaseWait", "");
            ProgressOverlay.Visible = false;
            ScriptRunning = false;
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
            InfoPanel.Visible = !hidePanel;
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
        }

        [HandleMessage("ise:updatesettings", true)]
        protected virtual void UpdateSettings(ClientPipelineArgs args)
        {
            var settings = ApplicationSettings.GetInstance(ApplicationNames.ISE);
            var backgroundColor = OutputLine.ProcessHtmlColor(settings.BackgroundColor);
            var bottomPadding = CurrentVersion.IsAtLeast(SitecoreVersion.V80) ? 0 : 10;
            SheerResponse.Eval(
                $"spe.changeSettings('{settings.FontFamilyStyle}', {settings.FontSize}, " +
                $"'{backgroundColor}', {bottomPadding}," +
                $" {settings.LiveAutocompletion.ToString().ToLower()}," +
                $" {settings.PerTabOutput.ToString().ToLower()});");
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

        [HandleMessage("ise:immediatewindow", true)]
        protected virtual void ImmediateWindow(ClientPipelineArgs args)
        {
            if (ScriptSessionManager.SessionExists(Monitor.SessionID))
            {
                Context.ClientPage.Start(this, nameof(ImmediateWindowPipeline));
            }
        }

        public void ImmediateWindowPipeline(ClientPipelineArgs args)
        {
            if (!args.IsPostBack)
            {
                Monitor.Active = false;
                var session = ScriptSessionManager.GetSession(Monitor.SessionID);
                var url = new UrlString(UIUtil.GetUri("control:PowerShellConsole"));
                url.Parameters["id"] = session.Key;
                url.Parameters["debug"] = "true";
                TypeResolver.Resolve<IImmediateDebugWindowLauncher>().ShowImmediateWindow(url);
                args.WaitForPostBack(true);
            }
            else
            {
                Monitor.Active = true;
            }
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