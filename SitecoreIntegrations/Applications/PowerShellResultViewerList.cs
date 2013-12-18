using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using Cognifide.PowerShell.PowerShellIntegrations.Host;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
using Cognifide.PowerShell.SitecoreIntegrations.Controls;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Shell.Framework;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;
using Sitecore.Web.UI.WebControls.Ribbons;
using Image = Sitecore.Web.UI.HtmlControls.Image;

namespace Cognifide.PowerShell.SitecoreIntegrations.Applications
{
    public class PowerShellResultViewerList : BaseForm, IHasCommandContext
    {
        protected JobMonitor Monitor;
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
        protected ThemedImage InfoIcon;

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
                    Monitor = new JobMonitor {ID = "Monitor"};
                    Context.ClientPage.Controls.Add(Monitor);
                }
                else
                {
                    Monitor = (JobMonitor) Context.ClientPage.FindControl("Monitor");
                }
            }

            if (Context.ClientPage.IsEvent)
                return;

            string sid = WebUtil.GetQueryString("sid");
            ListViewer.ContextId = sid;
            ListViewer.Refresh();
            ChangePage(ListViewer.CurrentPage);
            Monitor.JobFinished += MonitorJobFinished;
            Monitor.JobDisappeared += MonitorJobFinished;
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
            UpdateRibbon();
        }

        [HandleMessage("pslv:filter")]
        public void Filter()
        {
            ListViewer.Filter = Context.ClientPage.Request.Params["Input_Filter"];
            ListViewer.Refresh();
            ChangePage(1);
        }

        public void OnDoubleClick()
        {
            if (ListViewer.GetSelectedItems().Length <= 0) return;
            int clickedId = Int32.Parse(ListViewer.GetSelectedItems()[0].Value);
            object originalData = ListViewer.Data.Data[clickedId].Original;
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
            Item item = ScriptItemId == null ? null : Client.ContentDatabase.GetItem(ScriptItemId);

            switch (message.Name)
            {
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

        private void ExportResults(Message message)
        {
            Database scriptDb = Database.GetDatabase(message.Arguments["scriptDb"]);
            Item scriptItem = scriptDb.GetItem(message.Arguments["scriptID"]);
            using (var session = new ScriptSession(ApplicationNames.Default))
            {
                String script = (scriptItem.Fields[ScriptItemFieldNames.Script] != null)
                    ? scriptItem.Fields[ScriptItemFieldNames.Script].Value
                    : string.Empty;
                List<object> results = ListViewer.GetFilteredItems().Select(p => p.Original).ToList();
                session.SetVariable("resultSet", results);
                string formatProperty = ListViewer.Data.Property
                    .Where(p =>
                    {
                        var label = string.Empty;
                        if (p is Hashtable)
                        {
                            Hashtable h = p as Hashtable;
                            if(h.ContainsKey("Name"))
                            {
                                if (!h.ContainsKey("Label"))
                                {
                                    h.Add("Label",h["Name"]);
                                }
                            }
                            label = h["Label"].ToString().ToLower(CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            label = p.ToString().ToLower(CultureInfo.InvariantCulture);
                        }
                        return label != "icon" && label != "__icon";
                    })
                    .Select(p =>
                    {
                        if (p is Hashtable)
                        {
                            var v = p as Hashtable;
                            return "@{Label=\"" + v["Label"] + "\";Expression={" + v["Expression"] + "}},";
                        }
                        return "@{Label=\"" + p + "\";Expression={$_." + p + "}},";
                    }).Aggregate((a, b) => a + b).TrimEnd(',');
                session.SetVariable("formatProperty", formatProperty);
                session.SetVariable("title", ListViewer.Data.Title);
                session.SetVariable("infoTitle", ListViewer.Data.InfoTitle);
                session.SetVariable("infoDescription", ListViewer.Data.InfoDescription);
                session.SetVariable("actionData", ListViewer.Data.ActionData);
                string result = session.ExecuteScriptPart(script, false).Last().ToString();
                SheerResponse.Download(result);
            }
        }

        private void ChangePage(int newPage)
        {
            int count = ListViewer.FilteredCount;
            int pageSize = ListViewer.Data.PageSize;
            int pageCount = count/pageSize + ((count%pageSize > 0) ? 1 : 0);
            newPage = Math.Min(Math.Max(1, newPage), pageCount);
            ListViewer.CurrentPage = newPage;
            ItemCount.Text = count.ToString(CultureInfo.InvariantCulture);
            CurrentPage.Text = ListViewer.CurrentPage.ToString(CultureInfo.InvariantCulture);
            PageCount.Text = (pageCount).ToString(CultureInfo.InvariantCulture);
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
            Item item = ScriptItemId == null ? null : Client.ContentDatabase.GetItem(ScriptItemId);
            ribbon.CommandContext = new CommandContext(item);
            ribbon.ShowContextualTabs = false;
            Item obj2 = Context.Database.GetItem("/sitecore/content/Applications/PowerShell/PowerShellListView/Ribbon");
            Error.AssertItemFound(obj2, "/sitecore/content/Applications/PowerShell/PowerShellListView/Ribbon");
            ribbon.CommandContext.RibbonSourceUri = obj2.Uri;
            if (ListViewer.Data.Data.Count > 0)
            {
                ribbon.CommandContext.Parameters.Add("type", ListViewer.Data.Data[0].Original.GetType().Name);
                ribbon.CommandContext.CustomData = ListViewer.Data.Data[0].Original;
            }
            RibbonPanel.InnerHtml = HtmlUtil.RenderControl(ribbon);
        }

        public CommandContext GetCommandContext()
        {
            Item itemNotNull = Client.CoreDatabase.GetItem("{98EF2F7D-C17A-4A53-83A8-0EA2C0517AD6}");
                // /sitecore/content/Applications/PowerShell/PowerShellGridView/Ribbon
            var context = new CommandContext {RibbonSourceUri = itemNotNull.Uri};
            return context;
        }

        private void ListViewAction(Message message)
        {
            Database scriptDb = Database.GetDatabase(message.Arguments["scriptDb"]);
            Item scriptItem = scriptDb.GetItem(message.Arguments["scriptID"]);

            ScriptSession scriptSession =
                ScriptSessionManager.GetSession(scriptItem[ScriptItemFieldNames.PersistentSessionId]);

            String script = (scriptItem.Fields[ScriptItemFieldNames.Script] != null)
                ? scriptItem.Fields[ScriptItemFieldNames.Script].Value
                : string.Empty;
            List<object> results = ListViewer.SelectedItems.Select(p =>
            {
                int id = Int32.Parse(p.Value);
                return ListViewer.Data.Data[id].Original;
            }).ToList();
            scriptSession.SetVariable("resultSet", results);
            scriptSession.SetVariable("formatProperty", ListViewer.Data.Property);

            var parameters = new object[]
            {
                scriptSession,
                script
            };

            var progressBoxRunner = new ScriptRunner(ExecuteInternal, parameters,
                string.IsNullOrEmpty(scriptItem[ScriptItemFieldNames.PersistentSessionId]));
            Monitor.Start("ScriptExecution", "UI", progressBoxRunner.Run);
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