using System;
using System.Collections.Specialized;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Shell.Framework;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;
using Sitecore.Web.UI.XmlControls;

namespace Cognifide.PowerShell.Client.Applications
{
    public class PowerShellScriptBrowser : DialogForm
    {
        // Fields
        protected XmlControl Dialog;
        protected Edit Filename;
        protected DataContext ScriptDataContext;
        protected TreeviewEx Treeview;
        protected Border NamePanel;

        // Methods
        private Item GetCurrentItem(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            string str = message["id"];
            Language language = Context.Language;
            Item folder = ScriptDataContext.GetFolder();
            if (folder != null)
            {
                language = folder.Language;
            }
            if (!string.IsNullOrEmpty(str))
            {
                return Context.ContentDatabase.Items[str, language];
            }
            return folder;
        }

        public override void HandleMessage(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            Dispatcher.Dispatch(message, GetCurrentItem(message));
            base.HandleMessage(message);
        }

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            if (!Context.ClientPage.IsEvent)
            {
                ScriptDataContext.GetFromQueryString();
                string queryString = WebUtil.GetQueryString("db");
                if (queryString.Length > 0)
                {
                    if (!string.IsNullOrEmpty(ScriptDataContext.Parameters))
                    {
                        ScriptDataContext.Parameters = ScriptDataContext.Parameters + "&databaseName=" + queryString;
                    }
                    else
                    {
                        ScriptDataContext.Parameters = "databaseName=" + queryString;
                    }
                }
                Context.ClientPage.ServerProperties["id"] = WebUtil.GetQueryString("id");
                string icon = WebUtil.GetQueryString("ic");
                if (icon.Length > 0)
                {
                    Dialog["Icon"] = icon;
                }
                string header = WebUtil.SafeEncode(WebUtil.GetQueryString("he"));
                if (header.Length > 0)
                {
                    Dialog["Header"] = header;
                }
                string text = WebUtil.SafeEncode(WebUtil.GetQueryString("txt"));
                if (text.Length > 0)
                {
                    Dialog["Text"] = text;
                }
                string buttonText = WebUtil.SafeEncode(WebUtil.GetQueryString("btn"));
                if (buttonText.Length > 0)
                {
                    Dialog["OKButton"] = buttonText;
                }
                Item folder = ScriptDataContext.GetFolder();
                Assert.IsNotNull(folder, "Item not found");
                //this.Filename.Value = this.ShortenPath(folder.Paths.Path);
                NamePanel.Visible = WebUtil.SafeEncode(WebUtil.GetQueryString("opn")) != "1";
            }
        }

        protected override void OnOK(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull(args, "args");
            Item selectedItem = Treeview.GetSelectionItem();
            string scriptName = Filename.Value;

            bool opening = WebUtil.SafeEncode(WebUtil.GetQueryString("opn")) == "1";


            if (opening)
            {
                if (selectedItem == null || selectedItem.TemplateName != "PowerShell Script")
                {
                    SheerResponse.Alert(
                        "Select a script you want to open.",
                        new string[0]);
                }
                else
                {
                    Context.ClientPage.ClientResponse.SetDialogValue(selectedItem.Paths.Path);
                    SheerResponse.CloseWindow();
                }
            }
            else
            {
                if (selectedItem == null)
                {
                    SheerResponse.Alert(
                        "Select a library where you want your script saved and specify a name for your script.",
                        new string[0]);
                }
                else if (selectedItem.TemplateName == "PowerShell Script Library" && scriptName.Length == 0)
                {
                    SheerResponse.Alert("Specify a name for your script.", new string[0]);
                }
                else if (selectedItem.TemplateName == "PowerShell Script") // selected existing script.
                {
                    var parameters = new NameValueCollection();
                    parameters["fullPath"] = selectedItem.Paths.Path;
                    parameters["message"] = "Are you sure you want to overwrite the selected script?";
                    Context.ClientPage.Start(this, "OverwriteScript", parameters);
                }
                else
                {
                    string fullPath = selectedItem.Paths.Path + "/" + Filename.Value;
                    if (selectedItem.Children[scriptName] != null)
                    {
                        var parameters = new NameValueCollection();
                        parameters["fullPath"] = fullPath;
                        parameters["message"] =
                            "Script with that name already exists, are you sure you want to overwrite the script?";
                        Context.ClientPage.Start(this, "OverwriteScript", parameters);
                    }
                    else
                    {
                        Context.ClientPage.ClientResponse.SetDialogValue(fullPath);
                        base.OnOK(sender, args);
                    }
                }
            }
        }

        protected void OverwriteScript(ClientPipelineArgs args)
        {
            if (args.IsPostBack)
            {
                if (args.Result == "yes")
                {
                    string fullPath = args.Parameters["fullPath"];
                    Context.ClientPage.ClientResponse.SetDialogValue(fullPath);
                    base.OnOK(this, args);
                }
            }
            else
            {
                SheerResponse.Confirm(args.Parameters["message"]);
                args.WaitForPostBack();
            }
        }

        protected void SelectTreeNode()
        {
            Item selectionItem = Treeview.GetSelectionItem();
            OK.Enabled = selectionItem != null;
        }

        protected void TreeviewDblClick()
        {
            OnOK(this, EventArgs.Empty);
        }
    }
}