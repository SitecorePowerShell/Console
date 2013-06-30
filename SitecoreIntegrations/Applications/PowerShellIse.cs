using System;
using System.Text;
using Cognifide.PowerShell.PowerShellIntegrations;
using Cognifide.PowerShell.PowerShellIntegrations.Host;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
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

namespace Cognifide.PowerShell.SitecoreIntegrations.Applications
{
    public class PowerShellIse : BaseForm
    {
        private static readonly string[] pleaseWaitMessages =
        {
            "the architects are still drafting",
            "the bits are breeding",
            "we're running the script as fast as we can",
            "pay no attention to the man behind the curtain",
            "in the mean time enjoy the elevator music",
            "while the little elves run your script",
            "a few commandlets tried to escape, but we caught them",
            "and dream of a faster computer",
            "would you like fries with that?",
            "checking the gravitational constant in your locale",
            "go ahead -- hold your breath",
            "at least you're not on hold",
            "hum something loud while others stare",
            "you're not in Kansas any more",
            "the server is powered by a potato and two electrodes",
            "I love you just the way you are",
            "while I use your script to take over the world",
            "we're testing your patience",
            "as if you had any other choice",
            "would you take a moment to fill our user survey?",
            "but don't think of purple hippos",
            "why don't you make me a sandwich in the mean time?",
            "while the satellite moves into position",
            "the bits are flowing slowly today",
            "dig on the 'X' for buried treasure... ARRR!",
            "it's still faster than YOU would do it manually!",
            "you don't suffer from ADHD after all... Me neith-!... oh look a bunny... What was I doing again? Oh, right. Here we go."
            ,
            "the last time I tried this, the script didn't survive. Let's hope it works better this time.",
            "testing script on Timmy... ... ... We're going to need another Timmy.",
            "I should have had a V8 this morning.",
            "my other wait message is much faster. You should try that one instead.",
            "the version I have of this in testing has much funnier wait messages.",
            "happy Elf and Sad Elf are talking about your script. ",
            "all the elves are on break right now. Please hold.",
            "just a sec, I know your data is here somewhere",
            "measuring the cable length to fetch your data...",
            "while I do things you dont really wanna know about",
            "oiling clockworks",
            "hitting your keyboard won't make me run it any faster",
            "ensuring everything works perfectly",
            "on no! Look out! Behind you!",
            "preparing to spin you around rapidly",
            "dusting off spellbooks",
            "HELP!, I'm being held hostage, and forced to run scripts!",
            "Searching for answer to life, the universe, and everything",
            "while the gods contemplate your fate...",
            "waiting for the system admin to hit enter...",
            "paging for the system admin",
            "warming up the processors",
            "reconfiguring the office coffee machine",
            "re-calibrating the internet",
            "I'm working... no, just kidding",
            "So, how are you?",
            "are your shoelaces tied?",
            "working... unlike you!",
            "doing something useful...",
            "oh, yeah, comments! Good idea!",
            "prepare for awesomeness!",
            "it's not you. It's me.",
            "ouch! Careful where you point that thing!",
            "attentively (which is what you agreed to in the Terms and Conditions)",
            "QUIET !!! I'm trying to think here!",
            "counting backwards from infinity",
            "who is this General Failure and why is he reading my hard disk?",
            "testing for perfection",
            "deterministically simulating the future",
            "embiggening prototypes",
            "So, do you come here often?",
            "Your script is important to us. Please hold.",
            "damn it! I've lost it again. Searching...",
            "commencing infinite loop (this may take some time)"
        };

        protected DataContext DataContext;
        protected TreePicker DataSource;
        protected Combobox Databases;
        protected Memo Editor;
        protected Action HasFile;
        //protected Literal StatusText;
        protected JobMonitor Monitor;
        protected Scrollbox Result;
        protected Border RibbonPanel;
        //protected Literal TipText;
        protected bool ScriptRunning { get; set; }
        public ApplicationSettings Settings { get; set; }

        public string ParentFrameName
        {
            get { return StringUtil.GetString(ServerProperties["ParentFrameName"]); }
            set { ServerProperties["ParentFrameName"] = value; }
        }

        public string JobHandle
        {
            get { return StringUtil.GetString(ServerProperties["jobHandle"]); }
            set { ServerProperties["jobHandle"] = value; }
        }

        public string ResultValue
        {
            get { return StringUtil.GetString(ServerProperties["ResultValue"]); }
            set { ServerProperties["ResultValue"] = value; }
        }


        /// <summary>
        ///     Gets or sets the item ID.
        /// </summary>
        /// <value>
        ///     The item ID.
        /// </value>
        public static string ItemId
        {
            get { return StringUtil.GetString(Context.ClientPage.ServerProperties["ItemID"]); }
            set { Context.ClientPage.ServerProperties["ItemID"] = value; }
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
                ItemId = itemId;
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
            Item item = ItemId == null ? null : Client.ContentDatabase.GetItem(ItemId);

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

        [HandleMessage("ise:new", true)]
        protected void NewScript(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (args.IsPostBack)
            {
                if (!args.HasResult)
                    return;
                LoadItem(args.Result);
            }
            else
            {
                const string header = "Open Script";
                const string text = "Select the script item that you want to open.";
                const string icon = "powershell/48x48/script.png";
                const string button = "Open";
                const string root = "/sitecore/system/Modules/PowerShell/Script Library";
                const string selected = "/sitecore/system/Modules/PowerShell/Script Library/";

                string str = selected;
                if (selected.EndsWith("/"))
                {
                    Item obj = Context.ContentDatabase.Items[StringUtil.Left(selected, selected.Length - 1)];
                    if (obj != null)
                        str = obj.ID.ToString();
                }
                var urlString = new UrlString("/sitecore/shell/Applications/Item browser.aspx");
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
                ItemId = scriptItem.ID.ToString();
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
            if (string.IsNullOrEmpty(ItemId))
            {
                SaveAs(args);
            }
            else
            {
                Item scriptItem = Client.ContentDatabase.GetItem(new ID(ItemId));
                if (scriptItem == null)
                    return;
                scriptItem.Edit(
                    editArgs => { scriptItem.Fields[ScriptItemFieldNames.Script].Value = Editor.Value; });
            }
        }

        [HandleMessage("ise:reload", true)]
        protected void ReloadItem(ClientPipelineArgs args)
        {
            LoadItem(ItemId);
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
                ItemId = scriptItem.ID.ToString();
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
                    Context.Job.Status.Result = string.Format("<pre>{0}</pre>", output);
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

        [HandleMessage("ise:run", true)]
        protected virtual void ClientExecute(ClientPipelineArgs args)
        {
            Settings = ApplicationSettings.GetInstance(ApplicationNames.IseConsole);
            var scriptSession = new ScriptSession(Settings.ApplicationName);

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
            UpdateRibbon();

            Settings = ApplicationSettings.GetInstance(ApplicationNames.IseConsole);
            var scriptSession = new ScriptSession(Settings.ApplicationName);
            string contextScript = ScriptSession.GetDataContextSwitch(DataContext.CurrentItem);

            var parameters = new object[]
                {
                    scriptSession,
                    contextScript
                };

            var progressBoxRunner = new ScriptRunner(ExecuteInternal, parameters);

            var rnd = new Random();
            Context.ClientPage.ClientResponse.SetInnerHtml("Result",
                                                           string.Format(
                                                               "<div align='Center' style='padding:32px 0px 32px 0px'>Please wait, {0}</br><img src='../../../../../Console/Assets/working.gif' alt='Working' style='padding:32px 0px 32px 0px'/></div>",
                                                               pleaseWaitMessages[
                                                                   rnd.Next(pleaseWaitMessages.Length - 1)]));
            Monitor.Start("ScriptExecution", "UI", progressBoxRunner.Run);
            JobHandle = Monitor.JobHandle.ToString();

            if (Settings.SaveLastScript)
            {
                Settings.Load();
                Settings.LastScript = Editor.Value;
                Settings.Save();
            }
        }

        [HandleMessage("ise:updateresults", true)]
        protected virtual void UpdateResults(ClientPipelineArgs args)
        {
            var result = JobManager.GetJob(Monitor.JobHandle).Status.Result as string;
            Context.ClientPage.ClientResponse.SetInnerHtml("Result", result ?? "Script finished - no results to display.");
            UpdateRibbon();
        }


        /// <summary>
        ///     Updates the ribbon.
        /// </summary>
        private void UpdateRibbon()
        {
            var ribbon = new Ribbon {ID = "PowerShellRibbon"};
            Item item = ItemId == null ? null : Client.ContentDatabase.GetItem(ItemId);
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