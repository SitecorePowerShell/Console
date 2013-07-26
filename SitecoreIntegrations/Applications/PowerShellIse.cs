using System;
using System.Text;
using System.Web;
using Cognifide.PowerShell.PowerShellIntegrations;
using Cognifide.PowerShell.PowerShellIntegrations.Host;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
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
    public class PowerShellIse : BaseForm, IHasCommandContext
    {
        protected DataContext DataContext;
        protected TreePicker DataSource;
        protected Combobox Databases;
        protected Memo Editor;
        protected Action HasFile;
        protected JobMonitor Monitor;
        protected Scrollbox Result;
        protected Border RibbonPanel;
        protected Border ProgressOverlay;
        protected Border ScriptResult;
        protected Border EnterScriptInfo;

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
            var itemNotNull = Client.CoreDatabase.GetItem("{FDD5B2D5-31BE-41C3-AA76-64E5CC63B187}"); // /sitecore/content/Applications/PowerShell/PowerShellIse/Ribbon
            var context = new CommandContext { RibbonSourceUri = itemNotNull.Uri };
            return context;
        }

        /// <summary>
        ///     Builds the databases.
        /// </summary>
        /// <param name="database">The database.</param>
        private void BuildDatabases(string database)
        {
            Assert.ArgumentNotNull(database, "database");
            foreach (string name in Factory.GetDatabaseNames())
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
                LoadItem(itemId);
            }
            var rnd = new Random();
/*
            TipText.Text = hintMessages[rnd.Next(hintMessages.Length - 1)];
            StatusText.Text = TipText.Text;
*/

            if (Monitor == null)
            {
                if (!Context.ClientPage.IsEvent)
                {
                    Monitor = new JobMonitor {ID = "Monitor"};
                    Context.ClientPage.Controls.Add(Monitor);
                }
                else
                {
                    Monitor = Context.ClientPage.FindControl("Monitor") as JobMonitor;
                }
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
            foreach (string key in message.Arguments.AllKeys)
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
                LoadItem(args.Result);
                UpdateRibbon();
            }
            else
            {
                Dialogs.BrowseItem("Open Script",
                                   "Select the script item that you want to open.",
                                   "powershell/48x48/script.png", "Open",
                                   "/sitecore/system/Modules/PowerShell/Script Library",
                                   "/sitecore/system/Modules/PowerShell/Script Library/");
                args.WaitForPostBack();
            }
        }

        [HandleMessage("item:load", true)]
        protected void LoadContentEditor(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            var parameters = new Sitecore.Text.UrlString();
            parameters.Add("id", args.Parameters["id"]);
            parameters.Add("fo", args.Parameters["id"]);
            Windows.RunApplication("Content Editor", parameters.ToString());
        }

        [HandleMessage("ise:new", true)]
        protected void NewScript(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            ScriptItemId = string.Empty;
            Editor.Value = string.Empty;
            SheerResponse.Eval("cognified.powershell.clearEditor();");
            EnterScriptInfo.Visible = true;
            ScriptResult.Value = string.Empty;
            ScriptResult.Visible = false;
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
                UpdateRibbon();
            }
            else
            {
                const string header = "Select Script Library";
                const string text = "Select the Library that you want to save your script to.";
                const string icon = "powershell/48x48/script.png";
                const string button = "Select";
                const string root = "/sitecore/system/Modules/PowerShell/Script Library";
                const string selected = "/sitecore/system/Modules/PowerShell/Script Library/";

                string str = selected;
                if (selected.EndsWith("/"))
                {
                    Item obj = Context.ContentDatabase.Items[StringUtil.Left(selected, selected.Length - 1)];
                    if (obj != null)
                        str = obj.ID.ToString();
                }
                var urlString = new UrlString(UIUtil.GetUri("control:PowerShellNewScript"));
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
            LoadItem(ScriptItemId);
        }

        private void LoadItem(string id)
        {
            Assert.ArgumentNotNull(id, "id");
            Item scriptItem = Client.ContentDatabase.GetItem(id);
            if (scriptItem == null)
                return;

            if (scriptItem.Fields[ScriptItemFieldNames.Script] != null)
            {
                Editor.Value = scriptItem.Fields[ScriptItemFieldNames.Script].Value;
                SheerResponse.Eval("cognified.powershell.updateEditor();");
                ScriptItemId = scriptItem.ID.ToString();
                UpdateRibbon();
            }
            else
            {
                SheerResponse.Alert("The item cannot contain a script.", true);
            }
        }

        protected void ExecuteInternal(params object[] parameters)
        {
            var scriptSession = parameters[0] as ScriptSession;
            var contextScript = parameters[1] as string;
            var scriptItemId = parameters[2] as string;

            if (scriptSession == null || contextScript == null)
            {
                return;
            }

            try
            {
                scriptSession.ExecuteScriptPart(Settings.Prescript);
                scriptSession.ExecuteScriptPart(contextScript);
                scriptSession.ExecuteScriptPart(Editor.Value);
                var output = new StringBuilder(10240);
                if (scriptSession.Output != null)
                {
                    foreach (OutputLine outputLine in scriptSession.Output)
                    {
                        outputLine.GetHtmlLine(output);
                    }
                }
                if (Context.Job != null)
                {
                    JobContext.Flush();
                    Context.Job.Status.Result = string.Format("<pre>{0}</pre>", output);
                    JobContext.PostMessage("ise:updateresults");
                    JobContext.Flush();
                }
            }
            catch (Exception exc)
            {
                if (Context.Job != null)
                {
                    JobContext.Flush();
                    Context.Job.Status.Result =
                        string.Format("<pre style='background:red;'>{0}</pre>",
                                      scriptSession.GetExceptionString(exc));
                    JobContext.PostMessage("ise:updateresults");
                    JobContext.Flush();
                }
            }
        }

        [HandleMessage("ise:run", true)]
        protected virtual void ClientExecute(ClientPipelineArgs args)
        {
            Settings = ApplicationSettings.GetInstance(ApplicationNames.IseConsole);
            var scriptSession = new ScriptSession(Settings.ApplicationName);
            EnterScriptInfo.Visible = false;

            try
            {
                scriptSession.ExecuteScriptPart(Settings.Prescript);
                scriptSession.SetItemLocationContext(DataContext.CurrentItem);
                scriptSession.ExecuteScriptPart(Editor.Value);
                var output = new StringBuilder(10240);

                if (scriptSession.Output != null)
                {
                    foreach (OutputLine outputLine in scriptSession.Output)
                    {
                        outputLine.GetHtmlLine(output);
                    }
                    Context.ClientPage.ClientResponse.SetInnerHtml("Result", output.ToString());
                }
            }
            catch (Exception exc)
            {
                Context.ClientPage.ClientResponse.SetInnerHtml("Result",
                                                               string.Format("<pre style='background:red;'>{0}</pre>",
                                                                             scriptSession.GetExceptionString(exc)));
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
            string contextScript = ScriptSession.GetDataContextSwitch(DataContext.CurrentItem);

            var parameters = new object[]
                {
                    scriptSession,
                    contextScript,
                    ScriptItemId
                };

            var progressBoxRunner = new ScriptRunner(ExecuteInternal, parameters);

            var rnd = new Random();
            Context.ClientPage.ClientResponse.SetInnerHtml(
                "ScriptResult",
                string.Format(
                    "<div align='Center' style='padding:32px 0px 32px 0px'>Please wait, {0}</br>" +
                    "<img src='../../../../../Console/Assets/working.gif' alt='Working' style='padding:32px 0px 32px 0px'/></div>", ExecutionMessages.PleaseWaitMessages[
                        rnd.Next(ExecutionMessages.PleaseWaitMessages.Length - 1)]));
            Monitor.Start("ScriptExecution", "UI", progressBoxRunner.Run);

            HttpContext.Current.Session[Monitor.JobHandle.ToString()] = scriptSession;

            if (Settings.SaveLastScript)
            {
                Settings.Load();
                Settings.LastScript = Editor.Value;
                Settings.Save();
            }
        }

        [HandleMessage("ise:abort", true)]
        protected virtual void JobAbort(ClientPipelineArgs args)
        {
            var currentSession = HttpContext.Current.Session[Monitor.JobHandle.ToString()] as ScriptSession;
            currentSession.Abort();
            ScriptRunning = false;
            EnterScriptInfo.Visible = false;
            UpdateRibbon();
        }

        [HandleMessage("ise:updateresults", true)]
        protected virtual void UpdateResults(ClientPipelineArgs args)
        {
            var result = JobManager.GetJob(Monitor.JobHandle).Status.Result as string;
            HttpContext.Current.Session.Remove(Monitor.JobHandle.ToString());
            Context.ClientPage.ClientResponse.SetInnerHtml("ScriptResult", result ?? "Script finished - no results to display.");
            ProgressOverlay.Visible = false;
            ScriptResult.Visible = true;

            UpdateRibbon();
        }

        [HandleMessage("ise:updateprogress",true)]
        protected virtual void UpdateProgress(ClientPipelineArgs args)
        {
            ScriptResult.Visible = true;
            ProgressOverlay.Visible = true;
            var sb = new StringBuilder();
            sb.AppendFormat("<h2>{0}</h2>", args.Parameters["Activity"]);
            if (!string.IsNullOrEmpty(args.Parameters["CurrentOperation"]))
            {
                sb.AppendFormat("<p><strong>Operation</strong>: {0}</p>", args.Parameters["CurrentOperation"]);
            }
            if (!string.IsNullOrEmpty(args.Parameters["StatusDescription"]))
            {
                sb.AppendFormat("<p><strong>Status</strong>: {0}</p>", args.Parameters["StatusDescription"]);
            }

            if (!string.IsNullOrEmpty(args.Parameters["PercentComplete"]))
            {
                int percentComplete = Int32.Parse(args.Parameters["PercentComplete"]);
                if (percentComplete > -1)
                    sb.AppendFormat("<p><strong>Progress</strong>: {0}%</p> <div id='progressbar'><div style='width:{0}%'></div></div>", percentComplete);
            }

            if (!string.IsNullOrEmpty(args.Parameters["SecondsRemaining"]))
            {
                int secondsRemaining = Int32.Parse(args.Parameters["SecondsRemaining"]);
                if (secondsRemaining > -1)
                    sb.AppendFormat("<p><strong>Time Remaining</strong>:{0}seconds</p>", secondsRemaining);
            }
            Progress.Text = sb.ToString();
        }


        [HandleMessage("ise:updateribbon")]
        private void UpdateRibbon(Message message)
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