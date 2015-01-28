using System;
using System.Linq;
using System.Text;
using System.Web;
using Cognifide.PowerShell.Client.Controls;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Utility;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Jobs;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Shell.Framework;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls.Ribbons;
using Action = Sitecore.Web.UI.HtmlControls.Action;

namespace Cognifide.PowerShell.Client.Applications
{
    public class PowerShellIse : BaseForm, IHasCommandContext, IPowerShellRunner
    {
        protected DataContext DataContext;
        protected TreePicker DataSource;
        protected Memo Editor;
        protected Border EnterScriptInfo;
        protected Action HasFile;
        protected Literal Progress;
        protected Border ProgressOverlay;
        protected Border Result;
        protected Border RibbonPanel;
        protected Literal ScriptName;
        protected Border ScriptResult;
        protected bool ScriptRunning { get; set; }
        public ApplicationSettings Settings { get; set; }

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

        public static Item ScriptItem
        {
            get
            {
                var scriptItemId = ScriptItemId;
                if (string.IsNullOrEmpty(scriptItemId))
                {
                    return null;
                }
                return Sitecore.Client.ContentDatabase.GetItem(new ID(scriptItemId));
            }
        }

        public static string CurrentSessionId
        {
            get { return StringUtil.GetString(Context.ClientPage.ServerProperties["CurrentSessionId"]); }
            set { Context.ClientPage.ServerProperties["CurrentSessionId"] = value; }
        }

        public CommandContext GetCommandContext()
        {
            var itemNotNull = Sitecore.Client.CoreDatabase.GetItem("{FDD5B2D5-31BE-41C3-AA76-64E5CC63B187}");
            // /sitecore/content/Applications/PowerShell/PowerShellIse/Ribbon
            var context = new CommandContext {RibbonSourceUri = itemNotNull.Uri};
            return context;
        }

        public SpeJobMonitor Monitor { get; private set; }

        /// <summary>
        ///     Raises the load event.
        /// </summary>
        /// <param name="e">
        ///     The <see cref="T:System.EventArgs" /> instance containing the event data.
        /// </param>
        protected override void OnLoad(EventArgs e)
        {
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

            if (Context.ClientPage.IsEvent)
                return;

            Settings = ApplicationSettings.GetInstance(ApplicationNames.IseConsole);

            if (Settings.SaveLastScript)
            {
                Editor.Value = Settings.LastScript;
            }

            var itemId = WebUtil.GetQueryString("id");
            if (itemId.Length > 0)
            {
                ScriptItemId = itemId;
                LoadItem(WebUtil.GetQueryString("db"), itemId);
            }

            Monitor.JobFinished += MonitorJobFinished;
            Monitor.JobDisappeared += MonitorJobFinished;

            DataContext.Parameters = "databasename=" + Sitecore.Client.ContentDatabase.Name;

            ParentFrameName = WebUtil.GetQueryString("pfn");
            UpdateRibbon();
        }

        private void MonitorJobFinished(object sender, EventArgs e)
        {
            ScriptRunning = false;
            UpdateRibbon();
        }

        /// <summary>
        ///     Handles the message.
        /// </summary>
        /// <param name="message">The message.</param>
        public override void HandleMessage(Message message)
        {
            Error.AssertObject(message, "message");
            var item = ScriptItemId == null ? null : Sitecore.Client.ContentDatabase.GetItem(ScriptItemId);

            base.HandleMessage(message);

            var context = new CommandContext(item);
            foreach (var key in message.Arguments.AllKeys)
            {
                context.Parameters.Add(key, message.Arguments[key]);
            }

            if (!string.IsNullOrEmpty(ParentFrameName))
            {
                context.Parameters["ParentFramename"] = ParentFrameName;
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
                LoadItem(Sitecore.Client.ContentDatabase.Name, args.Result);
                UpdateRibbon();
            }
            else
            {
                const string header = "Open Script";
                const string text = "Select the script item that you want to open.";
                const string icon = "powershell/48x48/script.png";
                const string button = "Open";
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
                urlString.Append("he", header);
                urlString.Append("txt", text);
                urlString.Append("ic", icon);
                urlString.Append("btn", button);
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
            UpdateRibbon();
        }

        [HandleMessage("item:load", true)]
        protected void LoadContentEditor(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            var parameters = new UrlString();
            parameters.Add("id", args.Parameters["id"]);
            parameters.Add("fo", args.Parameters["id"]);
            Windows.RunApplication("Content Editor", parameters.ToString());
        }

        protected void MruUpdate(Item scriptItem)
        {
            Assert.ArgumentNotNull(scriptItem, "scriptItem");

            var db = scriptItem.Database.Name;
            var id = scriptItem.ID.ToString();
            var name = scriptItem.Name;
            var icon = scriptItem[FieldIDs.Icon];
            var scriptName = string.Format("{1}", db,
                scriptItem.Paths.Path.Substring(ApplicationSettings.ScriptLibraryPath.Length));
            ScriptName.Text = scriptName;
            SheerResponse.Eval(string.Format("cognifide.powershell.changeWindowTitle('{0}', false);", scriptName));
            var mruMenu = Sitecore.Client.CoreDatabase.GetItem("/sitecore/system/Modules/PowerShell/MRU") ??
                          Sitecore.Client.CoreDatabase.CreateItemPath("/sitecore/system/Modules/PowerShell/MRU");

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
                    openedScript["Display name"] = name;
                    openedScript["__Display name"] = name;
                    openedScript[FieldIDs.Sortorder] = "0";
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
            Editor.Value = string.Empty;
            EnterScriptInfo.Visible = true;
            ScriptResult.Value = string.Empty;
            ScriptResult.Visible = false;
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
                var itemTemplate =
                    Context.ContentDatabase.GetItem("/sitecore/templates/Modules/PowerShell Console/PowerShell Script");
                var libraryTemplate =
                    Context.ContentDatabase.GetItem(
                        "/sitecore/templates/Modules/PowerShell Console/PowerShell Script Library");
                DataContext.DisableEvents();
                var scriptItem = Context.ContentDatabase.CreateItemPath(args.Result, libraryTemplate, itemTemplate);
                DataContext.EnableEvents();
                ScriptItemId = scriptItem.ID.ToString();
                SaveItem(new ClientPipelineArgs());
                MruUpdate(scriptItem);
                UpdateRibbon();
            }
            else
            {
                const string header = "Select Script Library";
                const string text = "Select the Library that you want to save your script to.";
                const string icon = "powershell/48x48/script.png";
                const string button = "Select";
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
                urlString.Append("he", header);
                urlString.Append("txt", text);
                urlString.Append("ic", icon);
                urlString.Append("btn", button);
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
            }
        }

        [HandleMessage("ise:reload", true)]
        protected void ReloadItem(ClientPipelineArgs args)
        {
            LoadItem(Sitecore.Client.ContentDatabase.Name, ScriptItemId);
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
                MruUpdate(scriptItem);
                UpdateRibbon();
            }
            else
            {
                SheerResponse.Alert("The item cannot contain a script.", true);
            }
        }

        [HandleMessage("ise:run", true)]
        protected virtual void ClientExecute(ClientPipelineArgs args)
        {
            Settings = ApplicationSettings.GetInstance(ApplicationNames.IseConsole);
            using (var scriptSession = new ScriptSession(Settings.ApplicationName))
            {
                EnterScriptInfo.Visible = false;

                try
                {
                    scriptSession.ExecuteScriptPart(Settings.Prescript);
                    scriptSession.SetItemLocationContext(DataContext.CurrentItem);
                    scriptSession.SetExecutedScript(Sitecore.Client.ContentDatabase.Name, ScriptItemId);
                    scriptSession.ExecuteScriptPart(Editor.Value);

                    if (scriptSession.Output != null)
                    {
                        Context.ClientPage.ClientResponse.SetInnerHtml("Result", scriptSession.Output.ToHtml());
                    }
                }
                catch (Exception exc)
                {
                    Context.ClientPage.ClientResponse.SetInnerHtml("Result",
                        string.Format("<pre style='background:red;'>{0}</pre>",
                            scriptSession.GetExceptionString(exc)));
                }
            }
            if (Settings.SaveLastScript)
            {
                Settings.Load();
                Settings.LastScript = Editor.Value;
                Settings.Save();
            }
        }

        [HandleMessage("ise:execute", true)]
        protected virtual void JobExecute(ClientPipelineArgs args)
        {
            ScriptRunning = true;
            EnterScriptInfo.Visible = false;
            UpdateRibbon();

            ScriptSession scriptSession = null;
            
            string sessionName = CurrentSessionId;
            if (string.Equals(sessionName, StringTokens.PersistentSessionId, StringComparison.OrdinalIgnoreCase))
            {
                var script = ScriptItem;
                sessionName = script != null ? script[ScriptItemFieldNames.PersistentSessionId] : string.Empty;
            }

            var autoDispose = string.IsNullOrEmpty(sessionName);

            if (autoDispose)
            {
                Settings = ApplicationSettings.GetInstance(ApplicationNames.IseConsole,true);
                scriptSession = new ScriptSession(Settings.ApplicationName);
                scriptSession.SetItemLocationContext(DataContext.CurrentItem);
            }
            else
            {
                scriptSession = ScriptSessionManager.GetSession(sessionName, ApplicationNames.IseConsole, true);
                Settings = scriptSession.Settings;
            }

            scriptSession.SetExecutedScript(Sitecore.Client.ContentDatabase.Name, ScriptItemId);
            var parameters = new object[]
            {
                scriptSession,
                ScriptItemId
            };

            var progressBoxRunner = new ScriptRunner(ExecuteInternal, parameters, autoDispose);

            var rnd = new Random();
            Context.ClientPage.ClientResponse.SetInnerHtml(
                "ScriptResult",
                string.Format(
                    "<div id='ResultsClose' onclick='javascript:return cognifide.powershell.closeResults();' >x</div>" +
                    "<div align='Center' style='padding:32px 0px 32px 0px'>Please wait, {0}</br>" +
                    "<img src='../../../../../sitecore modules/PowerShell/Assets/working.gif' alt='Working' style='padding:32px 0px 32px 0px'/></div>",
                    ExecutionMessages.PleaseWaitMessages[
                        rnd.Next(ExecutionMessages.PleaseWaitMessages.Length - 1)]));
            Monitor.Start("ScriptExecution", "UI", progressBoxRunner.Run);

            HttpContext.Current.Session[Monitor.JobHandle.ToString()] = scriptSession;
            SheerResponse.Eval("cognifide.powershell.restoreResults();");

            if (Settings.SaveLastScript)
            {
                Settings.Load();
                Settings.LastScript = Editor.Value;
                Settings.Save();
            }
        }

        protected void ExecuteInternal(params object[] parameters)
        {
            var scriptSession = parameters[0] as ScriptSession;

            if (scriptSession == null)
            {
                return;
            }

            try
            {
                scriptSession.ExecuteScriptPart(Settings.Prescript);
                scriptSession.ExecuteScriptPart(Editor.Value);
                var output = new StringBuilder(10240);
                if (Context.Job != null)
                {
                    Context.Job.Status.Result = string.Format("<pre>{0}</pre>", scriptSession.Output.ToHtml());
                    JobContext.PostMessage("ise:updateresults");
                    JobContext.Flush();
                }
            }
            catch (Exception exc)
            {
                if (Context.Job != null)
                {
                    Context.Job.Status.Result =
                        string.Format("<pre style='background:red;'>{0}</pre>",
                            scriptSession.GetExceptionString(exc));
                    JobContext.PostMessage("ise:updateresults");
                    JobContext.Flush();
                }
            }
        }

        [HandleMessage("ise:abort", true)]
        protected virtual void JobAbort(ClientPipelineArgs args)
        {
            var currentSession = (ScriptSession) HttpContext.Current.Session[Monitor.JobHandle.ToString()];
            if (currentSession != null)
            {
                currentSession.Abort();
                if (currentSession.AutoDispose)
                {
                    currentSession.Dispose();
                }
            }
            ScriptRunning = false;
            EnterScriptInfo.Visible = false;
            UpdateRibbon();
        }

        [HandleMessage("ise:updateresults", true)]
        protected virtual void UpdateResults(ClientPipelineArgs args)
        {
            var result = JobManager.GetJob(Monitor.JobHandle).Status.Result as string;
            HttpContext.Current.Session.Remove(Monitor.JobHandle.ToString());
            Context.ClientPage.ClientResponse.SetInnerHtml("ScriptResult",
                "<div id='ResultsClose' onclick='javascript:return cognifide.powershell.closeResults();' >x</div>" +
                (result ?? "Script finished - no results to display."));
            ProgressOverlay.Visible = false;
            ScriptResult.Visible = true;

            UpdateRibbon();
        }

        [HandleMessage("ise:updateprogress", true)]
        protected virtual void UpdateProgress(ClientPipelineArgs args)
        {
            var showProgress =
                !string.Equals(args.Parameters["RecordType"], "Completed", StringComparison.OrdinalIgnoreCase);
            ScriptResult.Visible = !showProgress;
            ProgressOverlay.Visible = showProgress;
            var sb = new StringBuilder();
            if (showProgress)
            {
                ProgressOverlay.Visible = true;

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
                        sb.AppendFormat("<p><strong>{0:c} </strong> remaining.</p>",
                            new TimeSpan(0, 0, secondsRemaining));
                }

                if (!string.IsNullOrEmpty(args.Parameters["CurrentOperation"]))
                {
                    sb.AppendFormat("<p>{0}</p>", args.Parameters["CurrentOperation"]);
                }
            }

            Progress.Text = sb.ToString();
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
            var item = ScriptItemId == null ? null : Sitecore.Client.ContentDatabase.GetItem(ScriptItemId);
            ribbon.CommandContext = new CommandContext(item);
            ribbon.ShowContextualTabs = false;
            ribbon.CommandContext.Parameters["HasFile"] = HasFile.Disabled ? "0" : "1";
            ribbon.CommandContext.Parameters["ScriptRunning"] = ScriptRunning ? "1" : "0";
            ribbon.CommandContext.Parameters["currentSessionId"] = CurrentSessionId ?? string.Empty;
            var sessionName = CurrentSessionId ?? string.Empty;
            var persistentSessionId = sessionName; 
            if (string.Equals(sessionName, StringTokens.PersistentSessionId, StringComparison.OrdinalIgnoreCase))
            {
                var name =
                    item != null &&
                    !string.IsNullOrEmpty(item[ScriptItemFieldNames.PersistentSessionId])
                        ? item[ScriptItemFieldNames.PersistentSessionId]
                        : null;
                sessionName = string.Format("Script defined: {0}", name ?? "One-time session");
                persistentSessionId = name ?? string.Empty;
            }

            ribbon.CommandContext.Parameters["persistentSessionId"] = persistentSessionId;
            ribbon.CommandContext.Parameters["currentSessionName"] = string.IsNullOrEmpty(sessionName)
                ? "One-time session"
                : sessionName;

            var obj2 = Context.Database.GetItem("/sitecore/content/Applications/PowerShell/PowerShellIse/Ribbon");
            Error.AssertItemFound(obj2, "/sitecore/content/Applications/PowerShell/PowerShellIse/Ribbon");
            ribbon.CommandContext.RibbonSourceUri = obj2.Uri;

            ribbon.CommandContext.Parameters.Add("contextDB", DataContext.CurrentItem.Database.Name);
            ribbon.CommandContext.Parameters.Add("contextItem", DataContext.CurrentItem.ID.ToString());


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
            CurrentSessionId = args.Parameters["id"];
            UpdateRibbon();
        }

        [HandleMessage("ise:pickcontextitem", true)]
        protected void PickContextItem(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (args.IsPostBack)
            {
                if (!args.HasResult)
                    return;
                var newCurrentItem = PathUtilities.GetItem(args.Result, string.Empty, string.Empty);
                DataContext.Parameters = "databasename=" + newCurrentItem.Database.Name;
                DataContext.SetFolder(newCurrentItem.Uri);
                UpdateRibbon();
            }
            else
            {
                const string header = "Pick Context Item";
                const string text = "Select the item the script will be executed in the context of.";
                const string icon = "powershell/48x48/script.png";
                const string button = "Change";

                var urlString = new UrlString(UIUtil.GetUri("control:PowerShellItemSelector"));
                urlString.Append("id", DataContext.CurrentItem.ID.ToString());
                urlString.Append("db", DataContext.CurrentItem.Database.Name);
                urlString.Append("ds", text);
                urlString.Append("te", header);
                urlString.Append("ic", icon);
                urlString.Append("ok", button);
                SheerResponse.ShowModalDialog(urlString.ToString(), true);
                args.WaitForPostBack();
            }
        }
    }
}