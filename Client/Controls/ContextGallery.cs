using System;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Shell.Applications.ContentEditor;
using Sitecore.Shell.Applications.ContentManager.Galleries;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;

namespace Cognifide.PowerShell.Client.Controls
{
    public class ContextGallery : GalleryForm
    {
        // Fields
        protected DataContext ContentDataContext;
        protected TreeviewEx ContentTreeview;
        protected Combobox Databases;
        protected string InitialDatabase;
        protected GalleryMenu Options;
        protected Menu RecentMenu;
        protected Tab RecentTab;

        public override void HandleMessage(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            if (message.Name != "event:click" && message.Name != "datacontext:changed" && message.Name != "event:change")
            {
                Invoke(message, true);
            }
        }

        // Methods
        protected void ContentTreeview_Click()
        {
            var folder = ContentDataContext.GetFolder();
            if (folder != null)
            {
                Load(folder.Uri.ToString());
            }
        }

        protected void Load(string uri)
        {
            Assert.ArgumentNotNull(uri, "uri");
            var uri2 = ItemUri.Parse(uri);
            if (uri2 != null)
            {
                var item = Database.GetItem(uri2);
                if (item != null)
                {
                    RecentDocuments.AddToRecentDocuments(item.Uri);
                    SheerResponse.Eval(
                        string.Concat("scForm.getParentForm().invoke(\"ise:setcontextitem(id=", item.ID, ",language=",
                            item.Language, ",version=", item.Version, ",db=", item.Database.Name, ")\")"));
                }
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            if (!Context.ClientPage.IsEvent)
            {
                Item item;
                Item item2;
                Item[] itemArray;
                RenderRecent();

                var db = WebUtil.GetQueryString("contextDb");
                var itemId = WebUtil.GetQueryString("contextItem");
                InitialDatabase = string.IsNullOrEmpty(db) ? Sitecore.Client.ContentDatabase.Name : db;

                BuildDatabases();

                ContentDataContext.BeginUpdate();
                ContentDataContext.Parameters = "databasename=" + InitialDatabase;

                if (!string.IsNullOrEmpty(itemId) && !string.IsNullOrEmpty(db))
                {
                    ContentDataContext.SetFolder(Factory.GetDatabase(db).GetItem(itemId).Uri);
                }
                ContentDataContext.EndUpdate();
                ContentTreeview.RefreshRoot();

                ContentDataContext.GetFromQueryString();
                ContentDataContext.GetState(out item, out item2, out itemArray);
                if (itemArray.Length > 0)
                {
                    ContentDataContext.Folder = itemArray[0].ID.ToString();
                }
                var menuItem =
                    Sitecore.Client.CoreDatabase.GetItem(
                        "/sitecore/content/Applications/PowerShell/PowerShellIse/Menus/Context");
                if ((menuItem != null) && item.HasChildren)
                {
                    var queryString = WebUtil.GetQueryString("id");
                    Options.AddFromDataSource(menuItem, queryString, new CommandContext(menuItem));
                }
            }
        }

        private void RenderRecent()
        {
            SheerResponse.DisableOutput();
            try
            {
                foreach (var uri in RecentDocuments.GetRecentDocuments())
                {
                    var item = Database.GetItem(uri);
                    if (item != null)
                    {
                        var child = new MenuItem();
                        RecentMenu.Controls.Add(child);
                        child.Header = item.DisplayName;
                        child.Icon = item.Appearance.Icon;
                        child.Click = "Load(\"" + uri + "\")";
                    }
                }
                if (RecentMenu.Controls.Count == 0)
                {
                    RecentMenu.Add("(empty)", string.Empty, string.Empty);
                }
            }
            finally
            {
                SheerResponse.EnableOutput();
            }
        }

        private void BuildDatabases()
        {
            foreach (var str in Factory.GetDatabaseNames())
            {
                if (!Sitecore.Client.GetDatabaseNotNull(str).ReadOnly)
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
            ContentDataContext.BeginUpdate();
            ContentDataContext.Parameters = "databasename=" + name;
            ContentDataContext.EndUpdate();
            ContentTreeview.RefreshRoot();
        }
    }
}