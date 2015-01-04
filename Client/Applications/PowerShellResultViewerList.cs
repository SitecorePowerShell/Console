using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Web;
using Cognifide.PowerShell.Client.Controls;
using Cognifide.PowerShell.Commandlets.Interactive;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Settings;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;
using Sitecore.Web.UI.WebControls.Ribbons;
using Image = Sitecore.Web.UI.HtmlControls.Image;

namespace Cognifide.PowerShell.Client.Applications
{
    public class PowerShellResultViewerList : BaseForm, IHasCommandContext, IPowerShellRunner
    {
        public SpeJobMonitor Monitor { get; private set; }
        protected Border RibbonPanel;
        protected Border StatusBar;
        protected Literal StatusTip;
        protected Image RefreshHint;
        protected PowerShellListView ListViewer;
        protected Literal ItemCount;
        protected Literal CurrentPage;
        protected Literal PageCount;
        protected Literal InfoTitle;
        protected Literal Description;
        protected GridPanel InfoPanel;
        protected Image InfoIcon;
        protected Border LvProgressOverlay;
        protected Literal Progress;

        protected bool ScriptRunning { get; set; }

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
        public string ScriptItemId
        {
            get { return StringUtil.GetString(Context.ClientPage.ServerProperties["ItemID"]); }
            set { Context.ClientPage.ServerProperties["ItemID"] = value; }
        }

        
        /// <summary>
        ///     Gets or sets the item ID.
        /// </summary>
        /// <value>
        ///     The item ID.
        /// </value>
        public string ScriptSessionId
        {
            get { return StringUtil.GetString(Context.ClientPage.ServerProperties["ScriptSessionId"]); }
            set { Context.ClientPage.ServerProperties["ScriptSessionId"] = value; }
        }

        [HandleMessage("ise:updateprogress", true)]
        protected virtual void UpdateProgress(ClientPipelineArgs args)
        {
            bool showProgress =
                !string.Equals(args.Parameters["RecordType"], "Completed", StringComparison.OrdinalIgnoreCase);
            LvProgressOverlay.Visible = showProgress;
            var sb = new StringBuilder();
            if (showProgress)
            {

                sb.AppendFormat("<div>{0}</div>", args.Parameters["Activity"]);
                sb.AppendFormat("<div>");
                if (!string.IsNullOrEmpty(args.Parameters["StatusDescription"]))
                {
                    sb.AppendFormat("{0}, ", args.Parameters["StatusDescription"]);
                }

                if (!string.IsNullOrEmpty(args.Parameters["SecondsRemaining"]))
                {
                    int secondsRemaining = Int32.Parse(args.Parameters["SecondsRemaining"]);
                    if (secondsRemaining > -1)
                        sb.AppendFormat("<strong>{0:c} </strong> remaining, ", new TimeSpan(0, 0, secondsRemaining));
                }

                if (!string.IsNullOrEmpty(args.Parameters["CurrentOperation"]))
                {
                    sb.AppendFormat("{0}", args.Parameters["CurrentOperation"]);
                }

                sb.AppendFormat(".</div>");
                if (!string.IsNullOrEmpty(args.Parameters["PercentComplete"]))
                {
                    int percentComplete = Int32.Parse(args.Parameters["PercentComplete"]);
                    if (percentComplete > -1)
                        sb.AppendFormat("<div id='lvProgressbar'><div style='width:{0}%'></div></div>", percentComplete);
                }
            }
            Progress.Text = sb.ToString();
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

            Monitor.JobFinished += MonitorJobFinished;
            Monitor.JobDisappeared += MonitorJobFinished;

            if (Context.ClientPage.IsEvent)
                return;

            string sid = WebUtil.GetQueryString("sid");
            ListViewer.ContextId = sid;
            ListViewer.Refresh();
            ChangePage(ListViewer.CurrentPage);
            ListViewer.View = "Details";
            ListViewer.DblClick = "OnDoubleClick";
            string infoTitle = ListViewer.Data.InfoTitle;
            string infoDescription = ListViewer.Data.InfoDescription;

            if (string.IsNullOrEmpty(infoTitle) && string.IsNullOrEmpty(infoDescription))
            {
                InfoPanel.Visible = false;
            }
            else
            {
                InfoTitle.Text = infoTitle ?? string.Empty;
                Description.Text = infoDescription ?? string.Empty;
                if (!string.IsNullOrEmpty(ListViewer.Data.Icon))
                {
                    InfoIcon.Src = ListViewer.Data.Icon;
                }
            }
            ParentFrameName = WebUtil.GetQueryString("pfn");
            UpdateRibbon();
        }

        private void MonitorJobFinished(object sender, EventArgs e)
        {

            var session = ScriptSessionManager.GetSession(ScriptSessionId);
            if (session.CloseRunner)
            {
                session.CloseRunner = false;
                Windows.Close();
            }
            else
            {
                UpdateList(ScriptSessionId);
                UpdateRibbon();
            }
            if (session.AutoDispose)
            {
                ScriptSessionManager.RemoveSession(session);
            }
        }

        [HandleMessage("pslv:filter")]
        public void Filter()
        {
            ListViewer.Filter = Context.ClientPage.Request.Params["Input_Filter"];
            ChangePage(1);
        }

        public void OnDoubleClick()
        {
            if (ListViewer.GetSelectedItems().Length <= 0) return;
            int clickedId = Int32.Parse(ListViewer.GetSelectedItems()[0].Value);
            object originalData = ListViewer.Data.Data.Where(p => p.Id == clickedId).FirstOrDefault().Original;
            if (originalData is Item)
            {
                var clickedItem = originalData as Item;
                var urlParams = new UrlString();
                urlParams.Add("id", clickedItem.ID.ToString());
                urlParams.Add("fo", clickedItem.ID.ToString());
                urlParams.Add("la", clickedItem.Language.Name);
                urlParams.Add("vs", clickedItem.Version.Number.ToString(CultureInfo.InvariantCulture));
                urlParams.Add("sc_content", clickedItem.Database.Name);
                Windows.RunApplication("Content editor", urlParams.ToString());
            }
            ListViewer.Refresh();
        }

        /// <summary>
        ///     Handles the message.
        /// </summary>
        /// <param name="message">The message.</param>
        public override void HandleMessage(Message message)
        {
            Error.AssertObject(message, "message");
            Item item = ScriptItemId == null ? null : Sitecore.Client.ContentDatabase.GetItem(ScriptItemId);

            switch (message.Name)
            {
                case ("pslv:update"):
                    UpdateList(message.Arguments["ScriptSession.Id"]);
                    return;
                case ("pslv:filter"):
                    Filter();
                    message.CancelBubble = true;
                    message.CancelDispatch = true;
                    return;
                case ("pslvnav:first"):
                    ChangePage(0);
                    return;
                case ("pslvnav:last"):
                    ChangePage(Int32.MaxValue);
                    return;
                case ("pslvnav:previous"):
                    ChangePage(ListViewer.CurrentPage - 1);
                    ListViewer.Refresh();
                    return;
                case ("pslvnav:next"):
                    ChangePage(ListViewer.CurrentPage + 1);
                    return;
                case ("export:results"):
                    ExportResults(message);
                    return;
                case ("listview:action"):
                    ListViewAction(message);
                    return;
                default:
                    base.HandleMessage(message);
                    break;
            }
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

        public void UpdateList(string sessionId)
        {
            var session = ScriptSessionManager.GetSession(sessionId);
            var varValue = session.GetVariable("allDataInternal").BaseObject();
            ListViewer.Data.Data = varValue.BaseList<ShowListViewCommand.DataObject>();
            ListViewer.Refresh();
        }

        private void ExportResults(Message message)
        {
            Database scriptDb = Database.GetDatabase(message.Arguments["scriptDb"]);
            Item scriptItem = scriptDb.GetItem(message.Arguments["scriptID"]);
            using (var session = new ScriptSession(ApplicationNames.Default))
            {
                String script = (scriptItem.Fields[ScriptItemFieldNames.Script] != null)
                    ? scriptItem.Fields[ScriptItemFieldNames.Script].Value
                    : string.Empty;
                SetVariables(session);
                session.SetExecutedScript(scriptItem);
                string result = session.ExecuteScriptPart(script, false).Last().ToString();
                SheerResponse.Download(result);
            }
        }

        private void SetVariables(ScriptSession session)
        {
            //visible objects in string form for export
            List<PSObject> export = ListViewer.FilteredItems.Select(p =>
            {
                var psobj = new PSObject();
                foreach (var property in p.Display)
                {
                    psobj.Properties.Add(new PSNoteProperty(property.Key, property.Value));
                }
                return psobj;
            }).ToList();

            //selected original objects
            List<object> results = ListViewer.SelectedItems.Select(p =>
            {
                int id = Int32.Parse(p.Value);
                return ListViewer.Data.Data.Where(d => d.Id == id).Select(d => d.Original).First();
            }).ToList();

            session.SetVariable("filteredData", ListViewer.FilteredItems.Select(p => p.Original).ToList());
            session.SetVariable("resultSet", results);
            session.SetVariable("selectedData", results);
            session.SetVariable("exportData", export);
            session.SetVariable("allData", ListViewer.Data.Data.Select(p=> p.Original).ToList());
            session.SetVariable("allDataInternal", ListViewer.Data.Data);
            session.SetVariable("actionData", ListViewer.Data.ActionData);

            session.SetVariable("title", ListViewer.Data.Title);
            session.SetVariable("infoTitle", ListViewer.Data.InfoTitle);
            session.SetVariable("infoDescription", ListViewer.Data.InfoDescription);
            session.SetVariable("actionData", ListViewer.Data.ActionData);

            session.SetVariable("formatProperty", ListViewer.Data.Property.Cast<object>().ToArray());
            session.SetVariable("formatPropertyStr", ListViewer.Data.FormatProperty);
            session.SetVariable("exportProperty", ListViewer.Data.ExportProperty);

        }

        private void ChangePage(int newPage)
        {
            ListViewer.CurrentPage = newPage;
            ItemCount.Text = ListViewer.FilteredItems.Count.ToString(CultureInfo.InvariantCulture);
            CurrentPage.Text = ListViewer.CurrentPage.ToString(CultureInfo.InvariantCulture);
            PageCount.Text = (ListViewer.PageCount).ToString(CultureInfo.InvariantCulture);
            SheerResponse.Eval(string.Format("updateStatusBarCounters({0},{1},{2});", ItemCount.Text, CurrentPage.Text,
                PageCount.Text));
            ListViewer.Refresh();
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
            Item item = ScriptItemId == null ? null : Sitecore.Client.ContentDatabase.GetItem(ScriptItemId);
            ribbon.CommandContext = new CommandContext(item);
            ribbon.ShowContextualTabs = false;
            Item obj2 = Context.Database.GetItem("/sitecore/content/Applications/PowerShell/PowerShellListView/Ribbon");
            Error.AssertItemFound(obj2, "/sitecore/content/Applications/PowerShell/PowerShellListView/Ribbon");
            ribbon.CommandContext.RibbonSourceUri = obj2.Uri;
            if (ListViewer.Data.Data.Count > 0)
            {
                ribbon.CommandContext.Parameters.Add("type", ListViewer.Data.Data[0].Original.GetType().Name);
                ribbon.CommandContext.Parameters.Add("viewName", ListViewer.Data.ViewName);
                ribbon.CommandContext.Parameters.Add("ScriptRunning", ScriptRunning ? "1" : "0");
                ribbon.CommandContext.CustomData = ListViewer.Data.Data[0].Original;
            }
            RibbonPanel.InnerHtml = HtmlUtil.RenderControl(ribbon);
        }

        public CommandContext GetCommandContext()
        {
            Item itemNotNull = Sitecore.Client.CoreDatabase.GetItem("{98EF2F7D-C17A-4A53-83A8-0EA2C0517AD6}");
                // /sitecore/content/Applications/PowerShell/PowerShellGridView/Ribbon
            var context = new CommandContext {RibbonSourceUri = itemNotNull.Uri};
            return context;
        }

        private void ListViewAction(Message message)
        {
            ScriptRunning = true;
            UpdateRibbon();
            Database scriptDb = Database.GetDatabase(message.Arguments["scriptDb"]);
            Item scriptItem = scriptDb.GetItem(message.Arguments["scriptID"]);
            string sessionId = string.IsNullOrEmpty(ListViewer.Data.SessionId)
                ? scriptItem[ScriptItemFieldNames.PersistentSessionId]
                : ListViewer.Data.SessionId;
            ScriptSession scriptSession = ScriptSessionManager.GetSession(sessionId);

            String script = (scriptItem.Fields[ScriptItemFieldNames.Script] != null)
                ? scriptItem.Fields[ScriptItemFieldNames.Script].Value
                : string.Empty;
            SetVariables(scriptSession);
            scriptSession.SetExecutedScript(scriptItem);

            ScriptSessionId = scriptSession.ID;

            var parameters = new object[]
            {
                scriptSession,
                script
            };

            var progressBoxRunner = new ScriptRunner(ExecuteInternal, parameters,false);
            Monitor.Start("ScriptExecution", "UI", progressBoxRunner.Run);
            LvProgressOverlay.Visible = false;
            HttpContext.Current.Session[Monitor.JobHandle.ToString()] = scriptSession;
        }

        protected void ExecuteInternal(params object[] parameters)
        {
            var scriptSession = parameters[0] as ScriptSession;
            var script = parameters[1] as string;

            if (scriptSession == null || script == null)
            {
                return;
            }

            try
            {
                scriptSession.ExecuteScriptPart(script);
            }
            catch (Exception exc)
            {
                Log.Error(scriptSession.GetExceptionString(exc), exc);
            }
        }
    }
}