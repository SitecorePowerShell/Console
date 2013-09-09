using System;
using System.Globalization;
using System.Web.Configuration;
using Cognifide.PowerShell.SitecoreIntegrations.Controls;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Shell.Framework;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls.Ribbons;

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
                    Monitor = new JobMonitor { ID = "Monitor" };
                    Context.ClientPage.Controls.Add(Monitor);
                }
                else
                {
                    Monitor = (JobMonitor)Context.ClientPage.FindControl("Monitor");
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
            var clickedId = Int32.Parse(ListViewer.GetSelectedItems()[0].Value);
            var originalData = ListViewer.Data.Data[clickedId].Original;
            if(originalData is Item)
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
                default:
                    base.HandleMessage(message);
                    break;
            }
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

        private void ChangePage(int newPage)
        {
            int count = ListViewer.FilteredCount;
            int pageSize = ListViewer.Data.PageSize;
            int pageCount = count/pageSize + ((count%pageSize > 0) ? 1 : 0);            
            newPage = Math.Min(Math.Max(1, newPage),pageCount);
            ListViewer.CurrentPage = newPage;
            ItemCount.Text = count.ToString(CultureInfo.InvariantCulture);
            CurrentPage.Text = ListViewer.CurrentPage.ToString(CultureInfo.InvariantCulture);
            PageCount.Text = (pageCount).ToString(CultureInfo.InvariantCulture);
            SheerResponse.Eval(string.Format("updateStatusBarCounters({0},{1},{2});", ItemCount.Text, CurrentPage.Text, PageCount.Text));
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
            RibbonPanel.InnerHtml = HtmlUtil.RenderControl(ribbon);
        }

        public CommandContext GetCommandContext()
        {
            var itemNotNull = Client.CoreDatabase.GetItem("{98EF2F7D-C17A-4A53-83A8-0EA2C0517AD6}"); // /sitecore/content/Applications/PowerShell/PowerShellGridView/Ribbon
            var context = new CommandContext { RibbonSourceUri = itemNotNull.Uri };
            return context;
        }


    }
}