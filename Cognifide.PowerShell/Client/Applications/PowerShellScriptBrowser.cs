using System;
using System.Collections.Specialized;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Settings;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
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
        protected Border NamePanel;
        protected DataContext ScriptDataContext;
        protected TreeviewEx Treeview;
        protected Combobox Databases;
        protected string InitialDatabase;

        // Methods
        private Item GetCurrentItem(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            var id = message["id"];
            var language = Context.Language;
            var folder = ScriptDataContext.GetFolder();
            if (folder != null)
            {
                language = folder.Language;
            }
            return !string.IsNullOrEmpty(id) ? Context.ContentDatabase.Items[id, language] : folder;
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
                var db = WebUtil.GetQueryString("db");

                InitialDatabase = string.IsNullOrEmpty(db) ? ApplicationSettings.ScriptLibraryDb : db;

                BuildDatabases();

                if (db?.Length > 0)
                {
                    if (!string.IsNullOrEmpty(ScriptDataContext.Parameters))
                    {
                        ScriptDataContext.Parameters = ScriptDataContext.Parameters + "&databaseName=" + db;
                    }
                    else
                    {
                        ScriptDataContext.Parameters = "databaseName=" + db;
                    }
                }

                Context.ClientPage.ServerProperties["id"] = WebUtil.GetQueryString("id");
                var icon = WebUtil.GetQueryString("ic");
                if (icon.Length > 0)
                {
                    Dialog["Icon"] = icon;
                }
                var header = WebUtil.SafeEncode(WebUtil.GetQueryString("he"));
                if (header.Length > 0)
                {
                    Dialog["Header"] = header;
                }
                var text = WebUtil.SafeEncode(WebUtil.GetQueryString("txt"));
                if (text.Length > 0)
                {
                    Dialog["Text"] = text;
                }
                var buttonText = WebUtil.SafeEncode(WebUtil.GetQueryString("btn"));
                if (buttonText.Length > 0)
                {
                    Dialog["OKButton"] = buttonText;
                }
                var folder = ScriptDataContext.GetFolder();
                Assert.IsNotNull(folder, "Item not found");
                //this.Filename.Value = this.ShortenPath(folder.Paths.Path);
                NamePanel.Visible = WebUtil.SafeEncode(WebUtil.GetQueryString("opn")) != "1";
            }
        }

        protected override void OnOK(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull(args, "args");
            var selectedItem = Treeview.GetSelectionItem();
            var scriptName = Filename.Value;

            var opening = WebUtil.SafeEncode(WebUtil.GetQueryString("opn")) == "1";


            if (opening)
            {
                if (!selectedItem.IsPowerShellScript())
                {
                    SheerResponse.Alert(Texts.PowerShellScriptBrowser_Select_a_script);
                }
                else
                {
                    Context.ClientPage.ClientResponse.SetDialogValue(selectedItem.Database.Name + ":" +
                                                                     selectedItem.Paths.Path);
                    SheerResponse.CloseWindow();
                }
            }
            else
            {
                if (selectedItem == null)
                {
                    SheerResponse.Alert(Texts.PowerShellScriptBrowser_Select_a_library);
                }
                else if (selectedItem.IsPowerShellLibrary() && scriptName.Length == 0)
                {
                    SheerResponse.Alert(Texts.PowerShellScriptBrowser_Specify_a_name);
                }
                else if (selectedItem.IsPowerShellScript()) // selected existing script.
                {
                    var parameters = new NameValueCollection();
                    parameters["fullPath"] = $"{selectedItem.Database.Name}:{selectedItem.Paths.Path}";
                    parameters["message"] = Texts.PowerShellScriptBrowser_Are_you_sure_you_want_to_overwrite;
                    Context.ClientPage.Start(this, "OverwriteScript", parameters);
                }
                else
                {
                    var fullPath = selectedItem.Database.Name + ":" + selectedItem.Paths.Path + "/" + Filename.Value;
                    if (selectedItem.Children[scriptName] != null)
                    {
                        var parameters = new NameValueCollection();
                        parameters["fullPath"] = fullPath;
                        parameters["message"] = Texts.PowerShellScriptBrowser_Script_with_name_already_exists;
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
                    var fullPath = args.Parameters["fullPath"];
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
            var selectionItem = Treeview.GetSelectionItem();
            OK.Enabled = selectionItem != null;
        }

        protected void TreeviewDblClick()
        {
            OnOK(this, EventArgs.Empty);
        }

        private void BuildDatabases()
        {
            foreach (var str in Factory.GetDatabaseNames())
            {
                if (!string.Equals(str, "core", StringComparison.OrdinalIgnoreCase) &&
                    !Sitecore.Client.GetDatabaseNotNull(str).ReadOnly)
                {
                    var child = new ListItem();
                    Databases.Controls.Add(child);
                    child.ID = Control.GetUniqueID("ListItem");
                    child.Header = str;
                    child.Value = str;
                    child.Selected = str.Equals(InitialDatabase, StringComparison.InvariantCultureIgnoreCase);
                }
            }
        }

        protected void ChangeDatabase()
        {
            var name = Databases.SelectedItem.Value;
            ScriptDataContext.BeginUpdate();
            ScriptDataContext.Parameters = "databasename=" + name;
            ScriptDataContext.EndUpdate();
            Treeview.RefreshRoot();
        }

    }
}