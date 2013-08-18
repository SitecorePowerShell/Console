using System;
using System.Linq;
using System.Text;
using System.Web;
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
    public class PowerShellResultViewerGrid : BaseForm, IHasCommandContext
    {
        protected JobMonitor Monitor;
        protected Scrollbox Result;
        protected Border RibbonPanel;
        protected Border ProgressOverlay;
        protected Border ScriptResult;
        protected Border EnterScriptInfo;
        protected Border ScriptName;
        
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

            Monitor.JobFinished += MonitorJobFinished;
            Monitor.JobDisappeared += MonitorJobFinished;

            ParentFrameName = WebUtil.GetQueryString("pfn");
            UpdateRibbon();
        }

        private void MonitorJobFinished(object sender, EventArgs e)
        {
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
/*
            ribbon.CommandContext.Parameters["HasFile"] = HasFile.Disabled ? "0" : "1";
            ribbon.CommandContext.Parameters["ScriptRunning"] = ScriptRunning ? "1" : "0";
*/

            Item obj2 = Context.Database.GetItem("/sitecore/content/Applications/PowerShell/PowerShellGridView/Ribbon");
            Error.AssertItemFound(obj2, "/sitecore/content/Applications/PowerShell/PowerShellGridView/Ribbon");
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