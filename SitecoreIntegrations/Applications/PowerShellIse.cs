using System;
using System.Linq;
using System.Text;
using System.Web;
using Cognifide.PowerShell.PowerShellIntegrations;
using Cognifide.PowerShell.PowerShellIntegrations.Host;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
using Cognifide.PowerShell.SitecoreIntegrations.Controls;
using Sitecore;
using Sitecore.Collections;
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

namespace Cognifide.PowerShell.SitecoreIntegrations.Applications
{
    public class PowerShellIse : BaseForm, IHasCommandContext, IPowerShellRunner
    {
        protected DataContext DataContext;
        protected TreePicker DataSource;
        protected Combobox Databases;
        protected Memo Editor;
        protected Action HasFile;
        public SpeJobMonitor Monitor { get; private set; }
        protected Border Result;
        protected Border RibbonPanel;
        protected Border ProgressOverlay;
        protected Border ScriptResult;
        protected Border EnterScriptInfo;
        protected Border ScriptName;

        protected bool ScriptRunning { get; set; }
        public ApplicationSettings Settings { get; set; }
        protected Literal Progress;

        public string ParentFrameName
        {
            get { return StringUtil.GetString(ServerProperties["ParentFrameName"]); }
            set { ServerProperties["ParentFrameName"] = value; }
        }

        /// <summary>
        ///     Gets or sets the item ID.
        /// </summary>
        /// <value>
        ///     The item ID.
        /// </value>
        public static string ScriptItemId
        {
            get { return StringUtil.GetString(Context.ClientPage.ServerProperties["ItemID"]); }
            set { Context.ClientPage.ServerProperties["ItemID"] = value; }
        }

        public CommandContext GetCommandContext()
        {
            Item itemNotNull = Client.CoreDatabase.GetItem("{FDD5B2D5-31BE-41C3-AA76-64E5CC63B187}");
                // /sitecore/content/Applications/PowerShell/PowerShellIse/Ribbon
            var context = new CommandContext {RibbonSourceUri = itemNotNull.Uri};
            return context;
        }

        /// <summary>
        ///     Builds the databases.
        /// </summary>
        /// <param name="database">The database.</param>
        private void BuildDatabases(string database)
        {
            Assert.ArgumentNotNull(database, "database");
            foreach (var name in Factory.GetDatabaseNames())
            {
                if (!Factory.GetDatabase(name).ReadOnly)
                {
                    var listItem = new ListItem
                    {
                        ID = Control.GetUniqueID("ListItem"),
                        Header = name,
                        Value = name,
                        Selected = name == database
                    };
                    Databases.Controls.Add(listItem);
                }
            }
        }

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

            string itemId = WebUtil.GetQueryString("id");
            if (itemId.Length > 0)
            {
                ScriptItemId = itemId;
                LoadItem(WebUtil.GetQueryString("db"), itemId);
            }

            Monitor.JobFinished += MonitorJobFinished;
            Monitor.JobDisappeared += MonitorJobFinished;

            BuildDatabases(Client.ContentDatabase.Name);
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
            Item item = ScriptItemId == null ? null : Client.ContentDatabase.GetItem(ScriptItemId);

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
                LoadItem(Client.ContentDatabase.Name, args.Result);
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

                string str = selected;
                if (selected.EndsWith("/"))
                {
                    Item obj = Context.ContentDatabase.Items[StringUtil.Left(selected, selected.Length - 1)];
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

            string db = scriptItem.Database.Name;
            string id = scriptItem.ID.ToString();
            string name = scriptItem.Name;
            string icon = scriptItem[FieldIDs.Icon];
            ScriptName.Value = string.Format("{1}", db, scriptItem.Paths.Path.Substring(ApplicationSettings.ScriptLibraryPath.Length));
            SheerResponse.SetInnerHtml("ScriptName", ScriptName.Value);
            Item mruMenu = Client.CoreDatabase.GetItem("/sitecore/system/Modules/PowerShell/MRU") ??
                           Client.CoreDatabase.CreateItemPath("/sitecore/system/Modules/PowerShell/MRU");

            ChildList mruItems = mruMenu.Children;
            if (mruItems.Count == 0 || !(mruItems[0]["Message"].Contains(id)))
            {
                Item openedScript = mruItems.FirstOrDefault(mruItem => mruItem["Message"].Contains(id)) ??
                                    mruMenu.Add(Guid.NewGuid().ToString("n"), new TemplateID(ID.Parse("{998B965E-6AB8-4568-810F-8101D60D0CC3}")));
                openedScript.Edit(args =>
                {
                    openedScript["Message"] = string.Format("ise:mruopen(id={0},db={1})", id, db);
                    openedScript["Icon"] = icon;
                    openedScript["Display name"] = name;
                    openedScript["__Display name"] = name;
                    openedScript[FieldIDs.Sortorder] = "0";
                });

                int sortOrder = 1;
                foreach (Item mruItem in mruItems)
                {
                    if (sortOrder > 9)
                    {
                        mruItem.Delete();
                        continue;
                    }
                    if (!(mruItem["Message"].Contains(id)))
                    {
                        Item item = mruItem;
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
            SheerResponse.Eval("cognifide.powershell.clearEditor();");
            EnterScriptInfo.Visible = true;
            ScriptResult.Value = string.Empty;
            ScriptResult.Visible = false;
            SheerResponse.SetInnerHtml("ScriptName", "Unsaved script.");
            ScriptName.Value = string.Empty;
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
                Item itemTemplate =
                    Context.ContentDatabase.GetItem("/sitecore/templates/Modules/PowerShell Console/PowerShell Script");
                Item libraryTemplate =
                    Context.ContentDatabase.GetItem(
                        "/sitecore/templates/Modules/PowerShell Console/PowerShell Script Library");
                DataContext.DisableEvents();
                Item scriptItem = Context.ContentDatabase.CreateItemPath(args.Result, libraryTemplate, itemTemplate);
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

                string str = selected;
                if (selected.EndsWith("/"))
                {
                    Item obj = Context.ContentDatabase.Items[StringUtil.Left(selected, selected.Length - 1)];
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
                Item scriptItem = Client.ContentDatabase.GetItem(new ID(ScriptItemId));
                if (scriptItem == null)
                    return;
                scriptItem.Edit(
                    editArgs => { scriptItem.Fields[ScriptItemFieldNames.Script].Value = Editor.Value; });
            }
        }

        [HandleMessage("ise:reload", true)]
        protected void ReloadItem(ClientPipelineArgs args)
        {
            LoadItem(Client.ContentDatabase.Name, ScriptItemId);
        }

        private void LoadItem(string db, string id)
        {
            Assert.ArgumentNotNull(id, "id");
            Assert.ArgumentNotNull(db, "db");
            Item scriptItem = Factory.GetDatabase(db).GetItem(id);
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
                    scriptSession.SetExecutedScript(Client.ContentDatabase.Name, ScriptItemId);
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

            Settings = ApplicationSettings.GetInstance(ApplicationNames.IseConsole);
            var scriptSession = new ScriptSession(Settings.ApplicationName);
            scriptSession.SetItemLocationContext(DataContext.CurrentItem);
            scriptSession.SetExecutedScript(Client.ContentDatabase.Name, ScriptItemId);
            var parameters = new object[]
            {
                scriptSession,
                ScriptItemId
            };

            var progressBoxRunner = new ScriptRunner(ExecuteInternal, parameters, true);

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
                currentSession.Dispose();
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
            bool showProgress =
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
                    int percentComplete = Int32.Parse(args.Parameters["PercentComplete"]);
                    if (percentComplete > -1)
                        sb.AppendFormat("<div id='progressbar'><div style='width:{0}%'></div></div>", percentComplete);
                }

                if (!string.IsNullOrEmpty(args.Parameters["SecondsRemaining"]))
                {
                    int secondsRemaining = Int32.Parse(args.Parameters["SecondsRemaining"]);
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
            Item item = ScriptItemId == null ? null : Client.ContentDatabase.GetItem(ScriptItemId);
            ribbon.CommandContext = new CommandContext(item);
            ribbon.ShowContextualTabs = false;
            ribbon.CommandContext.Parameters["HasFile"] = HasFile.Disabled ? "0" : "1";
            ribbon.CommandContext.Parameters["ScriptRunning"] = ScriptRunning ? "1" : "0";

            Item obj2 = Context.Database.GetItem("/sitecore/content/Applications/PowerShell/PowerShellIse/Ribbon");
            Error.AssertItemFound(obj2, "/sitecore/content/Applications/PowerShell/PowerShellIse/Ribbon");
            ribbon.CommandContext.RibbonSourceUri = obj2.Uri;
            RibbonPanel.InnerHtml = HtmlUtil.RenderControl(ribbon);
        }

        /// <summary>
        ///     Changes the database.
        /// </summary>
        protected void ChangeDatabase()
        {
            DataContext.Parameters = "databasename=" + Databases.SelectedItem.Value;
        }

        [HandleMessage("item:updated", true)]
        protected void FieldEditor(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
        }
    }
}