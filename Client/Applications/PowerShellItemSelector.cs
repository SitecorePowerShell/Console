using System;
using System.Web;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Configuration;
using Sitecore.Controls;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Shell;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;

namespace Cognifide.PowerShell.Client.Applications
{
    public class PowerShellItemSelector : DialogPage
    {

        protected Combobox Databases;
        protected DataContext ItemDataContext;
        protected Literal DestinationPath;
        protected TreeviewEx ItemTreeview;
        protected Literal Result;
        protected Literal DialogHeader;
        protected Literal DialogDescription;
        protected Scrollbox ValuePanel;
        protected Button OKButton;
        protected Button CancelButton;
        protected bool ShowHints;
        protected string InitialDatabase;

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            if (Sitecore.Context.ClientPage.IsEvent)
                return;

            var db = WebUtil.GetQueryString("db");
            var itemId = WebUtil.GetQueryString("id");
            InitialDatabase = string.IsNullOrEmpty(db) ? Sitecore.Client.ContentDatabase.Name : db;
            
            BuildDatabases();

            ItemDataContext.BeginUpdate();
            ItemDataContext.Parameters = "databasename=" + InitialDatabase;

            if (!string.IsNullOrEmpty(itemId) && !string.IsNullOrEmpty(db))
            {
                ItemDataContext.SetFolder(Factory.GetDatabase(db).GetItem(itemId).Uri);
            }
            ItemDataContext.EndUpdate();
            ItemTreeview.RefreshRoot();

            HttpContext.Current.Response.AddHeader("X-UA-Compatible", "IE=edge");

            var title = WebUtil.GetQueryString("te");

            if (!string.IsNullOrEmpty(title))
            {
                DialogHeader.Text = title;
            }

            var description = WebUtil.GetQueryString("ds");
            if (!string.IsNullOrEmpty(description))
            {
                DialogDescription.Text = description;
            }
            var okText = WebUtil.GetQueryString("ob");
            if (!string.IsNullOrEmpty(okText))
            {
                OKButton.Header = okText;
            }

            var cancelText = WebUtil.GetQueryString("cb");
            if (!string.IsNullOrEmpty(cancelText))
            {
                CancelButton.Header = cancelText;
            }

        }

        private void BuildDatabases()
        {
            foreach (string str in Factory.GetDatabaseNames())
            {
                if (!Sitecore.Client.GetDatabaseNotNull(str).ReadOnly)
                {
                    ListItem child = new ListItem();
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
            string name = Databases.SelectedItem.Value;
            ItemDataContext.BeginUpdate();
            ItemDataContext.Parameters = "databasename=" + name;
            ItemDataContext.EndUpdate();
            ItemTreeview.RefreshRoot();
        }
        
        protected void OKClick()
        {
            SheerResponse.SetDialogValue(PathUtilities.GetItemPsPath(ItemDataContext.CurrentItem));
            SheerResponse.CloseWindow();
        }

        protected void CancelClick()
        {
            SheerResponse.CloseWindow();
        }

   }
}