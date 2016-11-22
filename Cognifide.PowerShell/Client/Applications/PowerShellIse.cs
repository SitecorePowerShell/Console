using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using Cognifide.PowerShell.Client.Controls;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Settings.Authorization;
using Cognifide.PowerShell.Core.VersionDecoupling;
using Cognifide.PowerShell.Core.VersionDecoupling.Interfaces;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.IO;
using Sitecore.Security.Accounts;
using Sitecore.Shell.Framework;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls.Ribbons;
using JobManager = Sitecore.Jobs.JobManager;

namespace Cognifide.PowerShell.Client.Applications
{
    public class PowerShellIse : BaseForm, IHasCommandContext, IPowerShellRunner
    {
        public const string DefaultSessionName = "ISE Editing Session";
        public const string DefaultUser = "CurrentUser";
        public const string DefaultLanguage = "CurrentLanguage";


        protected Memo Editor;
        protected Literal Progress;
        protected Border ProgressOverlay;
        protected Border Result;
        protected Border RibbonPanel;
        protected Literal ScriptName;
        protected Border ScriptResult;
        protected Memo SelectionText;
        protected Memo Breakpoints;
        public bool Debugging { get; set; }
        public bool InBreakpoint { get; set; }

        protected bool ScriptRunning
        {
            get { return StringUtil.GetString(Context.ClientPage.ServerProperties["ScriptRunning"]) == "1"; }
            set { Context.ClientPage.ServerProperties["ScriptRunning"] = value ? "1" : string.Empty; }
        }

        public string ParentFrameName
        {
            get { return StringUtil.GetString(ServerProperties["ParentFrameName"]); }
            set { ServerProperties["ParentFrameName"] = value; }
        }

        public static string ScriptItemId
        {
            get { return StringUtil.GetString(Context.ClientPage.ServerProperties["ItemID"]); }
            set { Context.ClientPage.ServerProperties["ItemID"] = value; }
        }

        public static string ScriptItemDb
        {
            get { return StringUtil.GetString(Context.ClientPage.ServerProperties["ItemDb"]); }
            set { Context.ClientPage.ServerProperties["ItemDb"] = value; }
        }

        public static string ContextItemId
        {
            get { return StringUtil.GetString(Context.ClientPage.ServerProperties["ContextItemID"]); }
            set { Context.ClientPage.ServerProperties["ContextItemID"] = value; }
        }

        public static string ContextItemDb
        {
            get { return StringUtil.GetString(Context.ClientPage.ServerProperties["ContextItemDb"]); }
            set { Context.ClientPage.ServerProperties["ContextItemDb"] = value; }
        }

        public static bool ScriptModified
        {
            get { return StringUtil.GetString(Context.ClientPage.ServerProperties["ScriptModified"]) == "1"; }
            set { Context.ClientPage.ServerProperties["ScriptModified"] = value ? "1" : string.Empty; }
        }

        public static bool UseContext
        {
            get
            {
                return string.IsNullOrEmpty(StringUtil.GetString(Context.ClientPage.ServerProperties["UseContext"]));
            }
            set { Context.ClientPage.ServerProperties["UseContext"] = value ? string.Empty : "0"; }
        }

        public static Item ScriptItem
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
                    : Factory.GetDatabase(ContextItemDb).GetItem(new ID(contextItemId));
            }
        }

        public static string CurrentSessionId
        {
            get { return StringUtil.GetString(Context.ClientPage.ServerProperties["CurrentSessionId"]); }
            set { Context.ClientPage.ServerProperties["CurrentSessionId"] = value; }
        }

        public static string CurrentUser
        {
            get { return StringUtil.GetString(Context.ClientPage.ServerProperties["CurrentUser"]); }
            set { Context.ClientPage.ServerProperties["CurrentUser"] = value; }
        }

        public static string CurrentLanguage
        {
            get { return StringUtil.GetString(Context.ClientPage.ServerProperties["CurrentLanguage"]); }
            set { Context.ClientPage.ServerProperties["CurrentLanguage"] = value; }
        }

        public SpeJobMonitor Monitor { get; private set; }

        public CommandContext GetCommandContext()
        {
            var itemNotNull = Sitecore.Client.CoreDatabase.GetItem("{FDD5B2D5-31BE-41C3-AA76-64E5CC63B187}");
            // /sitecore/content/Applications/PowerShell/PowerShellIse/Ribbon
            var context = new CommandContext {RibbonSourceUri = itemNotNull.Uri};
            return context;
        }

        public bool MonitorActive
        {
            get { return Monitor.Active; }
            set { Monitor.Active = value; }
        }

        protected override void OnLoad(EventArgs e)
        {
            Assert.CanRunApplication("PowerShell/PowerShellIse");
            Assert.IsTrue(ServiceAuthorizationManager.IsUserAuthorized(WebServiceSettings.ServiceClient, Context.User.Name, false), "Application access denied.");

            base.OnLoad(e);

            if (Monitor == null)
            {
                if (!Context.ClientPage.IsEvent)
                {
                    Monitor = new SpeJobMonitor {ID = "Monitor"};
                    Context.ClientPage.Controls.Add(Monitor);
                }
                else
                {
                    Monitor = (SpeJobMonitor) Context.ClientPage.FindControl("Monitor");
                }
            }
            Monitor.JobFinished += MonitorOnJobFinished;
            if (Context.ClientPage.IsEvent)
                return;

            var settings = ApplicationSettings.GetInstance(ApplicationNames.IseConsole);

            if (settings.SaveLastScript)
            {
                Editor.Value = settings.LastScript;
            }

            var itemId = WebUtil.GetQueryString("id");
            var itemDb = WebUtil.GetQueryString("db");
            if (itemId.Length > 0)
            {
                ScriptItemId = itemId;
                ScriptItemDb = itemDb;
                LoadItem(itemDb, itemId);
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
            LoadItem(args.Parameters["db"], args.Parameters["id"]);
        }

        
        [HandleMessage("ise:changecontextaccount", true)]
        protected void SecurityChangeContextAccount(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            args.CarryResultToNextProcessor = false;
            args.AbortPipeline();
            //LoadItem(args.Parameters["db"], args.Parameters["id"]);
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
            var scriptName = scriptItem.Paths.Path.Substring(ApplicationSettings.ScriptLibraryPath.Length);
            ScriptName.Text = scriptName;
            SheerResponse.Eval(string.Format("cognifide.powershell.changeWindowTitle('{0}', false);", scriptName));
            var mruMenu = ApplicationSettings.GetIseMruContainerItem();
            var mruItems = mruMenu.Children;
            if (mruItems.Count == 0 || !(mruItems[0]["Message"].Contains(id)))
            {
                var openedScript = mruItems.FirstOrDefault(mruItem => mruItem["Message"].Contains(id)) ??
                                   mruMenu.Add(Guid.NewGuid().ToString("n"),
                                       new TemplateID(ID.Parse("{998B965E-6AB8-4568-810F-8101D60D0CC3}")));
                openedScript.Edit(args =>
                {
                    openedScript["Message"] = string.Format("ise:mruopen(id={0},db={1})", id, db);
                    openedScript["Icon"] = icon;
                    openedScript["__Icon"] = icon;
                    openedScript["Display name"] = name;
                    openedScript["__Display name"] = name;
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
                            item[FieldIDs.Sortorder] = sortOrder.ToString("G");
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
            SheerResponse.Eval("cognifide.powershell.changeWindowTitle('Untitled', true);");
            UpdateRibbon();
        }

        [HandleMessage("ise:saveas", true)]
        protected void SaveAs(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (args.IsPostBack)
            {
                if (!args.HasResult)
                    return;

                var path = args.Result.Split(':');
                var db = Factory.GetDatabase(path[0]);
                var itemTemplate = db.GetTemplate("Modules/PowerShell Console/PowerShell Script");
                var libraryTemplate = db.GetTemplate("Modules/PowerShell Console/PowerShell Script Library");
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
                urlString.Append("txt", Texts.PowerShellIse_SaveAs_Select_the_Library_that_you_want_to_save_your_script_to_);
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
                var scriptItem = ScriptItem;
                if (scriptItem == null)
                    return;
                scriptItem.Edit(
                    editArgs => { scriptItem.Fields[ScriptItemFieldNames.Script].Value = Editor.Value; });
                SheerResponse.Eval("cognifide.powershell.updateModificationFlag(true);");
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
            if (scriptItem == null)
                return;

            if (scriptItem.Fields[ScriptItemFieldNames.Script] != null)
            {
                Editor.Value = scriptItem.Fields[ScriptItemFieldNames.Script].Value;
                SheerResponse.Eval("cognifide.powershell.updateEditor();");
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

        [HandleMessage("ise:run", true)]
        protected virtual void ClientExecute(ClientPipelineArgs args)
        {
            using (var scriptSession = ScriptSessionManager.NewSession(ApplicationNames.IseConsole, true))
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
                        Context.ClientPage.ClientResponse.SetInnerHtml("Result", scriptSession.Output.ToHtml());
                    }
                }
                catch (Exception exc)
                {
                    var result = string.Empty;
                    if (scriptSession.Output != null)
                    {
                        result += scriptSession.Output.ToHtml();
                    }
                    result += string.Format("<pre style='background:red;'>{0}</pre>",
                        ScriptSession.GetExceptionString(exc, ScriptSession.ExceptionStringFormat.Html));
                    Context.ClientPage.ClientResponse.SetInnerHtml("Result", result);
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
            JobExecuteScript(args, Editor.Value, false);
        }

        [HandleMessage("ise:debug", true)]
        protected virtual void Debug(ClientPipelineArgs args)
        {
            JobExecuteScript(args, Editor.Value, true);
        }

        [HandleMessage("ise:executeselection", true)]
        protected virtual void JobExecuteSelection(ClientPipelineArgs args)
        {
            JobExecuteScript(args, SelectionText.Value, false);
        }

        protected virtual void JobExecuteScript(ClientPipelineArgs args, string scriptToExecute, bool debug)
        {
            Debugging = debug;
            var sessionName = CurrentSessionId;
            if (string.Equals(sessionName, StringTokens.PersistentSessionId, StringComparison.OrdinalIgnoreCase))
            {
                var script = ScriptItem;
                sessionName = script != null ? script[ScriptItemFieldNames.PersistentSessionId] : string.Empty;
            }

            var autoDispose = string.IsNullOrEmpty(sessionName);
            var scriptSession = autoDispose
                ? ScriptSessionManager.NewSession(ApplicationNames.IseConsole, true)
                : ScriptSessionManager.GetSession(sessionName, ApplicationNames.IseConsole, true);

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

            scriptSession.SetExecutedScript(ScriptItem);
            var parameters = new object[]
            {
                scriptSession,
                scriptToExecute
            };

            var progressBoxRunner = new ScriptRunner(ExecuteInternal, scriptSession, scriptToExecute, autoDispose);

            var rnd = new Random();
            Context.ClientPage.ClientResponse.SetInnerHtml(
                "ScriptResult",
                string.Format(
                    "<div id='PleaseWait'>" +
                    "<img src='../../../../../sitecore modules/PowerShell/Assets/working.gif' alt='"+
                    Texts.PowerShellIse_JobExecuteScript_Working+
                    "' />" +
                    "<div>"+
                    Texts.PowerShellIse_JobExecuteScript_Please_wait___0_+
                    "</div>" +
                    "</div>" +
                    "<pre ID='ScriptResultCode'></pre>",
                    ExecutionMessages.PleaseWaitMessages[
                        rnd.Next(ExecutionMessages.PleaseWaitMessages.Length - 1)]));

            Context.ClientPage.ClientResponse.Eval("if(cognifide.powershell.preventCloseWhenRunning){cognifide.powershell.preventCloseWhenRunning(true);}");

            scriptSession.Debugging = debug;
            Monitor.Start("ScriptExecution", "UI", progressBoxRunner.Run,
                LanguageManager.IsValidLanguageName(CurrentLanguage)
                    ? LanguageManager.GetLanguage(CurrentLanguage)
                    : Context.Language,
                User.Exists(CurrentUser)
                    ? User.FromName(CurrentUser, true)
                    : Context.User);

            Monitor.SessionID = scriptSession.ID;
            SheerResponse.Eval("cognifide.powershell.restoreResults();");

            var settings = ApplicationSettings.GetInstance(ApplicationNames.IseConsole);
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
            ScriptSession scriptSession = ScriptSessionManager.NewSession(ApplicationNames.IseConsole, true);
            string scriptDb = args.Parameters["scriptDb"];
            string scriptItem = args.Parameters["scriptId"];
            Item script = Factory.GetDatabase(scriptDb).GetItem(scriptItem);
            scriptSession.SetVariable("scriptText", Editor.Value);
            scriptSession.SetVariable("selectionText", SelectionText.Value.Trim());
            scriptSession.SetVariable("scriptItem", ScriptItem);
            scriptSession.Interactive = true;
            JobExecuteScript(args, script[ScriptItemFieldNames.Script], scriptSession, true, false);
        }

        [HandleMessage("ise:pluginupdate", true)]
        protected void ConsumePluginResult(ClientPipelineArgs args)
        {
            var script = args.Parameters["script"];
            if (!string.IsNullOrEmpty(script))
            {
                Editor.Value = args.Parameters["script"];
            }
            SheerResponse.Eval("cognifide.powershell.updateEditor();");
        }

        [HandleMessage("pstaskmonitor:check", true)]
        protected void PrintOutput(ClientPipelineArgs args)
        {
            if (ScriptSessionManager.SessionExists(Monitor.SessionID))
            {
                var session = ScriptSessionManager.GetSession(Monitor.SessionID);
                var result = session.Output.GetHtmlUpdate();
                PrintSessionUpdate(result);
            }
        }

        private void PrintSessionUpdate(string result)
        {
            if (!string.IsNullOrEmpty(result))
            {
                result = HttpUtility.HtmlEncode(result.Replace("\r", "").Replace("\n", "<br/>")).Replace("\\", "&#92;");
                SheerResponse.Eval(string.Format("cognifide.powershell.appendOutput(\"{0}\");", result));
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
            if (ScriptSessionManager.SessionExists(Monitor.SessionID))
            {
                var currentSession = ScriptSessionManager.GetSession(Monitor.SessionID);

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
            ScriptRunning = false;
        }

        private void MonitorOnJobFinished(object sender, EventArgs eventArgs)
        {
            var args = eventArgs as SessionCompleteEventArgs;
            var result = args.RunnerOutput;
            if (result != null)
            {
                PrintSessionUpdate(result.Output);
            }

            if (result?.Exception != null)
            {
                var error = ScriptSession.GetExceptionString(result.Exception, ScriptSession.ExceptionStringFormat.Html);
                PrintSessionUpdate($"<pre style='background:red;'>{error}</pre>");
            }
            SheerResponse.SetInnerHtml("PleaseWait", "");
            ProgressOverlay.Visible = false;
            ScriptRunning = false;
            UpdateRibbon();
            SheerResponse.Eval("cognifide.powershell.scriptExecutionEnded()");
        }

        [HandleMessage("ise:updateprogress", true)]
        protected virtual void UpdateProgress(ClientPipelineArgs args)
        {
            var showProgress = ScriptRunning &&
                !string.Equals(args.Parameters["RecordType"], "Completed", StringComparison.OrdinalIgnoreCase);
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
                    var percentComplete = Int32.Parse(args.Parameters["PercentComplete"]);
                    if (percentComplete > -1)
                        sb.AppendFormat("<div id='progressbar'><div style='width:{0}%'></div></div>", percentComplete);
                }

                if (!string.IsNullOrEmpty(args.Parameters["SecondsRemaining"]))
                {
                    var secondsRemaining = Int32.Parse(args.Parameters["SecondsRemaining"]);
                    if (secondsRemaining > -1)
                        sb.AppendFormat("<p><strong>{0:c} </strong> "+
                            Texts.PowerShellIse_UpdateProgress_remaining+
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
            bool modified;
            Boolean.TryParse(message.Arguments["modified"], out modified);
            ScriptModified = modified;
            UpdateRibbon();
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
            var ribbon = new Ribbon {ID = "PowerShellRibbon"};
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
                    !string.IsNullOrEmpty(item[ScriptItemFieldNames.PersistentSessionId])
                        ? item[ScriptItemFieldNames.PersistentSessionId]
                        : null;
                sessionName = string.Format(Texts.PowerShellIse_UpdateRibbon_Script_defined___0_, name ?? Texts.PowerShellIse_UpdateRibbon_Single_execution);
                persistentSessionId = name ?? string.Empty;
            }

            ribbon.CommandContext.Parameters["persistentSessionId"] = persistentSessionId;
            ribbon.CommandContext.Parameters["currentSessionName"] = string.IsNullOrEmpty(sessionName)
                ? Texts.PowerShellIse_UpdateRibbon_Single_execution
                : (sessionName == DefaultSessionName)
                    ? Factory.GetDatabase("core")
                        .GetItem(
                            "/sitecore/content/Applications/PowerShell/PowerShellIse/Menus/Sessions/ISE editing session")?
                        .DisplayName ?? DefaultSessionName
                    : sessionName;
            var obj2 = Context.Database.GetItem("/sitecore/content/Applications/PowerShell/PowerShellIse/Ribbon");
            Error.AssertItemFound(obj2, "/sitecore/content/Applications/PowerShell/PowerShellIse/Ribbon");
            ribbon.CommandContext.RibbonSourceUri = obj2.Uri;

            ribbon.CommandContext.Parameters["currentUser"] = string.IsNullOrEmpty(CurrentUser) ? DefaultUser : CurrentUser;
            ribbon.CommandContext.Parameters["currentLanguage"] = string.IsNullOrEmpty(CurrentLanguage) ? DefaultLanguage : CurrentLanguage;

            ribbon.CommandContext.Parameters.Add("contextDB", UseContext ? ContextItemDb : string.Empty);
            ribbon.CommandContext.Parameters.Add("contextItem", UseContext ? ContextItemId : string.Empty);
            ribbon.CommandContext.Parameters.Add("scriptDB", ScriptItemDb);
            ribbon.CommandContext.Parameters.Add("scriptItem", ScriptItemId);
            RibbonPanel.InnerHtml = HtmlUtil.RenderControl(ribbon);
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
            CurrentSessionId = sessionId;
            SheerResponse.Eval($"cognifide.powershell.changeSessionId('{sessionId}');");
            UpdateRibbon();
        }

        [HandleMessage("ise:setlanguage", true)]
        protected void SetCurrentLanguage(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            var language = args.Parameters["language"];
            CurrentLanguage = language;
            new LanguageHistory().Add(language);
            UpdateRibbon();
        }

        [HandleMessage("ise:setuser", true)]
        protected void SetCurrentUser(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            var user = args.Parameters["user"];
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
            var settings = ApplicationSettings.GetInstance(ApplicationNames.IseConsole);
            var backgroundColor = OutputLine.ProcessHtmlColor(settings.BackgroundColor);
            var bottomPadding = CurrentVersion.IsAtLeast(SitecoreVersion.V80) ? 0 : 10;
            SheerResponse.Eval(
                $"cognifide.powershell.changeSettings('{settings.FontFamilyStyle}', {settings.FontSize}, '{backgroundColor}', {bottomPadding}, {settings.LiveAutocompletion.ToString().ToLower()});");
        }

        [HandleMessage("ise:setbreakpoint", true)]
        protected virtual void SetBreakpoint(ClientPipelineArgs args)
        {
            var line = args.Parameters["Line"];
            var action = args.Parameters["Action"];
            SheerResponse.Eval($"$ise(function() {{{{ cognifide.powershell.breakpointSet({line}, '{action}'); }}}});");
        }

        [HandleMessage("ise:togglebreakpoint", true)]
        protected virtual void ToggleRuntimeBreakpoint(ClientPipelineArgs args)
        {
            var line = Int32.Parse(args.Parameters["Line"]) + 1;
            var state = args.Parameters["State"] == "true";
            if (ScriptSessionManager.SessionExists(Monitor.SessionID))
            {
                var session = ScriptSessionManager.GetSession(Monitor.SessionID);
                var bPointScript = state
                    ? $"Set-PSBreakpoint -Script {session.DebugFile} -Line {line}"
                    : $"Get-PSBreakpoint -Script {session.DebugFile} | ? {{ $_.Line -eq {line}}} | Remove-PSBreakpoint";
                bPointScript += " | Out-Null";
                session.TryInvokeInRunningSession(bPointScript);
                InBreakpoint = false;
            }
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
                $"$ise(function() {{ cognifide.powershell.breakpointHit({line}, {column}, {endLine}, {endColumn}, '{jobId}'); }});");
        }


        [HandleMessage("ise:debugstart", true)]
        protected virtual void DebuggingStart(ClientPipelineArgs args)
        {
            SheerResponse.Eval("$ise(function() {{ cognifide.powershell.debugStart(); }});");
        }

        [HandleMessage("ise:debugend", true)]
        protected virtual void DebuggingEnd(ClientPipelineArgs args)
        {
            SheerResponse.Eval("$ise(function() {{ cognifide.powershell.debugStop(); }});");
        }

        [HandleMessage("ise:debugaction", true)]
        protected virtual void BreakpointAction(ClientPipelineArgs args)
        {
            if (ScriptSessionManager.SessionExists(Monitor.SessionID))
            {
                var session = ScriptSessionManager.GetSession(Monitor.SessionID);
                session.TryInvokeInRunningSession(args.Parameters["action"]);
                SheerResponse.Eval("$ise(function() { cognifide.powershell.breakpointHandled(); });");
                InBreakpoint = false;
            }
        }

        [HandleMessage("ise:immediatewindow", true)]
        protected virtual void ImmediateWindow(ClientPipelineArgs args)
        {
            if (ScriptSessionManager.SessionExists(Monitor.SessionID))
            {
                Context.ClientPage.Start(this, "ImmediateWindowPipeline");
            }
        }

        public void ImmediateWindowPipeline(ClientPipelineArgs args)
        {
            if (!args.IsPostBack)
            {
                Monitor.Active = false;
                var session = ScriptSessionManager.GetSession(Monitor.SessionID);
                UrlString url = new UrlString(UIUtil.GetUri("control:PowerShellConsole"));
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

    }
}