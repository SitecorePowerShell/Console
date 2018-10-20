﻿using System;
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
using Cognifide.PowerShell.Core.Settings.Authorization;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework;
using Sitecore.Shell.Framework.Commands;
using Sitecore.StringExtensions;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls.Ribbons;

namespace Cognifide.PowerShell.Client.Applications
{
    public class PowerShellResultViewerList : BaseForm, IHasCommandContext, IPowerShellRunner
    {
        protected Literal CurrentPage;
        protected Literal Description;
        protected Literal EmptyDataMessageText;
        protected Image EmptyIcon;
        protected Border EmptyPanel;
        protected Image InfoIcon;
        protected Border InfoPanel;
        protected Literal InfoTitle;
        protected Literal ItemCount;
        protected PowerShellListView ListViewer;
        protected Border LvProgressOverlay;
        protected Literal PageCount;
        protected Literal Progress;
        protected Image RefreshHint;
        protected Border RibbonPanel;
        protected Border StatusBar;
        protected Literal StatusTip;
        public SpeJobMonitor Monitor { get; private set; }
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

        /// <summary>
        ///     Indicates the window needs to be closed after the script finishes executing
        /// </summary>

        public CommandContext GetCommandContext()
        {
            var itemNotNull = Sitecore.Client.CoreDatabase.GetItem("{98EF2F7D-C17A-4A53-83A8-0EA2C0517AD6}");
            // /sitecore/content/Applications/PowerShell/PowerShellGridView/Ribbon
            var context = new CommandContext {RibbonSourceUri = itemNotNull.Uri};
            return context;
        }

        public bool MonitorActive
        {
            get { return Monitor.Active; }
            set { Monitor.Active = value; }
        }

        [HandleMessage("ise:updateprogress", true)]
        protected virtual void UpdateProgress(ClientPipelineArgs args)
        {
            var showProgress =
                !string.Equals(args.Parameters["RecordType"], "Completed", StringComparison.OrdinalIgnoreCase);
            LvProgressOverlay.Visible = showProgress;
            var sb = new StringBuilder();
            if (showProgress)
            {
                sb.AppendFormat("<div class='progressActivity'>{0}</div>", args.Parameters["Activity"]);
                sb.AppendFormat("<div class='progressStatus'>");
                if (!string.IsNullOrEmpty(args.Parameters["StatusDescription"]))
                {
                    sb.AppendFormat("{0}", args.Parameters["StatusDescription"]);
                }

                if (!string.IsNullOrEmpty(args.Parameters["SecondsRemaining"]))
                {
                    var secondsRemaining = Int32.Parse(args.Parameters["SecondsRemaining"]);
                    if (secondsRemaining > -1)
                        sb.AppendFormat("<strong class='progressRemaining'>, {0:c} </strong> " +
                            Texts.PowerShellResultViewerList_UpdateProgress_remaining,
                            new TimeSpan(0, 0, secondsRemaining));
                }

                if (!string.IsNullOrEmpty(args.Parameters["CurrentOperation"]))
                {
                    sb.AppendFormat(", {0}", args.Parameters["CurrentOperation"]);
                }

                sb.AppendFormat(".</div>");
                if (!string.IsNullOrEmpty(args.Parameters["PercentComplete"]))
                {
                    var percentComplete = Int32.Parse(args.Parameters["PercentComplete"]);
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
            Monitor.JobFinished += MonitorOnJobFinished;

            if (Context.ClientPage.IsEvent)
                return;

            var sid = WebUtil.GetQueryString("sid");
            ListViewer.ContextId = sid;

            if (ListViewer.Data == null)
            {
                UpdateInfoPanel(string.Empty, string.Empty, string.Empty, Texts.PowerShellResultViewerList_datamissing, string.Empty);
                return;
            }

            ListViewer.Refresh();
            UpdatePage(ListViewer.CurrentPage);
            ListViewer.View = "Details";
            ListViewer.DblClick = "OnDoubleClick";
            StatusBar.Visible = ListViewer.Data.VisibleFeatures.HasFlag(ShowListViewFeatures.StatusBar);
            var infoTitle = ListViewer.Data.InfoTitle;
            var infoDescription = ListViewer.Data.InfoDescription;
            var missingDataMessage = ListViewer.Data.MissingDataMessage;
            var missingDataIcon = ListViewer.Data.MissingDataIcon;
            var icon = ListViewer.Data.Icon;
            UpdateInfoPanel(infoTitle, infoDescription, icon, missingDataMessage, missingDataIcon);
            ParentFrameName = WebUtil.GetQueryString("pfn");
            UpdateRibbon();
        }

        private void UpdateInfoPanel(string infoTitle, string infoDescription, string icon, 
            string missingDataMessage, string missingDataIcon)
        {
            if (infoTitle.IsNullOrEmpty() && infoDescription.IsNullOrEmpty())
            {
                InfoPanel.Visible = false;
            }
            else
            {
                InfoTitle.Text = infoTitle ?? string.Empty;
                Description.Text = infoDescription ?? string.Empty;

                if (!icon.IsNullOrEmpty())
                {
                    Image image = new Image(icon)
                    {
                        Height = 24,
                        Width  = 24
                    };
                    Context.ClientPage.ClientResponse.SetOuterHtml("InfoIcon", image);
                }
            }

            if (!missingDataMessage.IsNullOrEmpty())
            {
                EmptyDataMessageText.Text = missingDataMessage;
            }

            if (!missingDataIcon.IsNullOrEmpty())
            {
                if (EmptyIcon == null)
                {
                    Image image = new Image(missingDataIcon);
                    Context.ClientPage.ClientResponse.SetOuterHtml("EmptyIcon", image);
                }
                else
                {
                    EmptyIcon.Src = missingDataIcon;
                }
            }

            if (ListViewer.Data?.Data == null || ListViewer.Data.Data.Count == 0)
            {
                ListViewer.Visible = false;
                EmptyPanel.Visible = true;
                EmptyDataMessageText.Visible = true;
            }
        }

        private void MonitorOnJobFinished(object sender, EventArgs eventArgs)
        {
            var args = eventArgs as SessionCompleteEventArgs;
            var result = args?.RunnerOutput;
            if (result?.CloseRunner ?? false)
            {
                Sitecore.Shell.Framework.Windows.Close();
            }
            else
            {
                UpdateRibbon();
            }
        }

        [HandleMessage("pslv:filter")]
        public void Filter()
        {
            ListViewer.Filter = Context.ClientPage.Request.Params["Input_Filter"];
            UpdatePage(1);
        }

        public void OnDoubleClick()
        {
            if (ListViewer.GetSelectedItems().Length <= 0) return;
            var clickedId = int.Parse(ListViewer.GetSelectedItems()[0].Value);
            var firstDataItem = ListViewer.Data.Data.FirstOrDefault(p => p.Id == clickedId);
            if (firstDataItem != null)
            {
                var originalData = firstDataItem.Original;
                if (originalData is PSObject customItem)
                {
                    var id = customItem.Properties["ID"]?.Value?.ToString();
                    if (!string.IsNullOrEmpty(id) && ID.IsID(id))
                    {
                        originalData = Sitecore.Client.ContentDatabase.GetItem(id);
                    }
                }

                if (originalData is Item item)
                {
                    var urlParams = new UrlString();
                    urlParams.Add("id", item.ID.ToString());
                    urlParams.Add("fo", item.ID.ToString());
                    urlParams.Add("la", item.Language.Name);
                    urlParams.Add("vs", item.Version.Number.ToString(CultureInfo.InvariantCulture));
                    urlParams.Add("sc_content", item.Database.Name);
                    Sitecore.Shell.Framework.Windows.RunApplication("Content editor", urlParams.ToString());
                }
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
            var item = ScriptItemId == null ? null : Sitecore.Client.ContentDatabase.GetItem(ScriptItemId);

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
                    UpdatePage(0);
                    return;
                case ("pslvnav:last"):
                    UpdatePage(int.MaxValue);
                    return;
                case ("pslvnav:previous"):
                    UpdatePage(ListViewer.CurrentPage - 1);
                    ListViewer.Refresh();
                    return;
                case ("pslvnav:next"):
                    UpdatePage(ListViewer.CurrentPage + 1);
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
            var key = $"allDataInternal|{sessionId}";
            if (HttpRuntime.Cache.Remove(key) is UpdateListViewCommand.UpdateListViewData updateData)
            {
                if (ListViewer.Data == null)
                {
                    SheerResponse.Alert(Texts.PowerShellResultViewerList_datamissing);
                    return;
                }
                ListViewer.Data.Data = updateData?.CumulativeData?.BaseList<BaseListViewCommand.DataObject>();
                UpdatePage(1);
                ListViewer.Refresh();
                UpdateInfoPanel(updateData.InfoTitleChange ? updateData.InfoTitle : InfoTitle.Text,
                    updateData.InfoDescriptionChange ? updateData.InfoDescription : Description.Text,
                    updateData.IconChange ? updateData.Icon : string.Empty,
                    updateData.MissingDataMessageChange ? updateData.MissingDataMessage : EmptyDataMessageText.Text,
                    updateData.MissingDataIconChange ? updateData.MissingDataIcon : string.Empty
                    );

            }
        }

        private void ExportResults(Message message)
        {
            var scriptDb = Database.GetDatabase(message.Arguments["scriptDb"]);
            var scriptItem = scriptDb.GetItem(message.Arguments["scriptID"]);
            var session = ScriptSessionManager.NewSession(ApplicationNames.Default, true);
            ExecuteScriptJob(scriptItem, session, message, true);
        }

        private void SetVariables(ScriptSession session, Message message)
        {
            //visible objects in string form for export
            var export = ListViewer.FilteredItems.Select(p =>
            {
                var psobj = new PSObject();
                foreach (var property in p.Display)
                {
                    psobj.Properties.Add(new PSNoteProperty(property.Key, property.Value));
                }
                return psobj;
            }).ToList();

            //selected original objects
            var results = ListViewer.SelectedItems.Select(p =>
            {
                var id = Int32.Parse(p.Value);
                return ListViewer.Data.Data.Where(d => d.Id == id).Select(d => d.Original).First();
            }).ToList();

            session.SetVariable("filteredData", ListViewer.FilteredItems.Select(p => p.Original).ToList());
            session.SetVariable("resultSet", results);
            session.SetVariable("selectedData", results);
            session.SetVariable("exportData", export);
            session.SetVariable("allData", ListViewer.Data.Data.Select(p => p.Original).ToList());
            session.SetVariable("allDataInternal", ListViewer.Data.Data);
            session.SetVariable("actionData", ListViewer.Data.ActionData);

            session.SetVariable("title", ListViewer.Data.Title);
            session.SetVariable("infoTitle", ListViewer.Data.InfoTitle);
            session.SetVariable("infoDescription", ListViewer.Data.InfoDescription);

            session.SetVariable("formatProperty", ListViewer.Data.Property.Cast<object>().ToArray());
            session.SetVariable("formatPropertyStr", ListViewer.Data.FormatProperty);
            session.SetVariable("exportProperty", ListViewer.Data.ExportProperty);
            if (message != null)
            {
                foreach (var key in message.Arguments.AllKeys)
                {
                    session.SetVariable(key, message.Arguments[key]);
                }
            }
        }

        private void UpdatePage(int newPage)
        {
            ListViewer.CurrentPage = newPage;
            ItemCount.Text = ListViewer.FilteredItems.Count.ToString(CultureInfo.InvariantCulture);
            CurrentPage.Text = ListViewer.CurrentPage.ToString(CultureInfo.InvariantCulture);
            PageCount.Text = (ListViewer.PageCount).ToString(CultureInfo.InvariantCulture);
            SheerResponse.Eval($"cognifide.powershell.updateStatusBarCounters({ItemCount.Text},{CurrentPage.Text},{PageCount.Text});");
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
            var item = ScriptItemId == null ? null : Sitecore.Client.ContentDatabase.GetItem(ScriptItemId);
            ribbon.CommandContext = new CommandContext(item);
            ribbon.ShowContextualTabs = false;
            var obj2 = Context.Database.GetItem("/sitecore/content/Applications/PowerShell/PowerShellListView/Ribbon");
            Error.AssertItemFound(obj2, "/sitecore/content/Applications/PowerShell/PowerShellListView/Ribbon");
            ribbon.CommandContext.RibbonSourceUri = obj2.Uri;
            if (ListViewer.Data.Data.Count > 0)
            {
                ribbon.CommandContext.Parameters.Add("type", ListViewer.Data.Data[0].Original.GetType().Name);
                ribbon.CommandContext.Parameters.Add("viewName", ListViewer.Data.ViewName);
                ribbon.CommandContext.Parameters.Add("ScriptRunning", ScriptRunning ? "1" : "0");
                ribbon.CommandContext.Parameters.Add("features", ListViewer.Data.VisibleFeatures.ToString("F"));
                ribbon.CommandContext.Parameters.Add("showPaging",
                    (ListViewer.Data.VisibleFeatures.HasFlag(ShowListViewFeatures.PagingAlways) ||
                     ListViewer.Data.Data.Count > ListViewer.Data.PageSize
                        ? "1"
                        : "0"));
                ribbon.CommandContext.Parameters.Add("showFilter",
                    ListViewer.Data.VisibleFeatures.HasFlag(ShowListViewFeatures.Filter) ? "1" : "0");
                
                ribbon.CommandContext.CustomData = ListViewer.Data.Data[0].Original;
            }
            RibbonPanel.InnerHtml = HtmlUtil.RenderControl(ribbon);
        }

        private void ListViewAction(Message message)
        {
            ScriptRunning = true;
            UpdateRibbon();
            var scriptDb = Database.GetDatabase(message.Arguments["scriptDb"]);
            var scriptItem = scriptDb.GetItem(message.Arguments["scriptID"]);
            if (!scriptItem.IsPowerShellScript())
            {
                return;
            }
            var sessionId = ListViewer.Data.SessionId.IfNullOrEmpty(scriptItem[Templates.Script.Fields.PersistentSessionId]);
            var scriptSession = ScriptSessionManager.GetSession(sessionId);
            ExecuteScriptJob(scriptItem, scriptSession, message, string.IsNullOrEmpty(sessionId));
        }

        private void ExecuteScriptJob(Item scriptItem, ScriptSession scriptSession, Message message, bool autoDispose)
        {
            if (!scriptItem.IsPowerShellScript())
            {
                SessionElevationErrors.OperationFailedWrongDataTemplate();
                return;
            }

            var script = scriptItem[Templates.Script.Fields.ScriptBody] ?? string.Empty;
            SetVariables(scriptSession, message);
            scriptSession.SetExecutedScript(scriptItem);
            scriptSession.Interactive = true;
            ScriptSessionId = scriptSession.ID;

            var progressBoxRunner = new ScriptRunner(ExecuteInternal, scriptSession, script, autoDispose);
            Monitor.Start($"SPE - \"{ListViewer?.Data?.Title}\" - \"{scriptItem?.Name}\"", "UI", progressBoxRunner.Run);
            LvProgressOverlay.Visible = false;
            Monitor.SessionID = scriptSession.ID;
        }

        protected void ExecuteInternal(ScriptSession scriptSession, string script)
        {
            scriptSession.ExecuteScriptPart(script);
        }
    }
}