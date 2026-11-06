using System;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using Sitecore.Shell.Applications.ContentEditor;
using Sitecore.Shell.Applications.ContentManager.Galleries;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;
using Spe.Core.Settings;

namespace Spe.Client.Controls
{
    public class PolicyGallery : GalleryForm
    {
        protected GalleryMenu Options;
        protected DataContext PolicyDataContext;
        protected TreeviewEx PolicyTreeview;

        public override void HandleMessage(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            if (message.Name != "event:click"
                && message.Name != "datacontext:changed"
                && message.Name != "event:change")
            {
                Invoke(message, true);
            }
        }

        protected void PolicyTreeview_Click()
        {
            var folder = PolicyDataContext.GetFolder();
            if (folder == null) return;
            if (folder.TemplateID != Templates.RemotingPolicy.Id)
            {
                // Clicking a folder just navigates the tree; don't select.
                return;
            }

            // Identify the policy by item ID, not name - names can repeat across
            // subfolders and would pick the wrong policy on the other side.
            SheerResponse.Eval(
                $"scForm.getParentForm().invoke(\"ise:setpolicy(id={folder.ID})\")");
        }

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            if (Context.ClientPage.IsEvent) return;

            var db = Factory.GetDatabase(ApplicationSettings.ScriptLibraryDb);
            if (db == null) return;

            PolicyDataContext.BeginUpdate();
            PolicyDataContext.DataViewName = "Master";
            PolicyDataContext.Parameters = "databasename=" + db.Name;
            PolicyDataContext.Root = ItemIDs.Policies.ToString();

            var currentPolicyId = WebUtil.GetQueryString("currentPolicy");
            Item initialFolder;
            using (new SecurityDisabler())
            {
                var root = db.GetItem(ItemIDs.Policies);
                if (!string.IsNullOrEmpty(currentPolicyId) && ID.TryParse(currentPolicyId, out var id))
                {
                    var selected = db.GetItem(id);
                    initialFolder = (selected != null && selected.TemplateID == Templates.RemotingPolicy.Id)
                        ? selected
                        : root;
                }
                else
                {
                    initialFolder = root;
                }
            }
            if (initialFolder != null)
            {
                PolicyDataContext.SetFolder(initialFolder.Uri);
            }

            PolicyDataContext.EndUpdate();
            PolicyTreeview.RefreshRoot();

            // Below the tree, show the "Other" section with clear/default actions
            // sourced from the core-DB menu item, matching the SessionIDGallery layout.
            var menu =
                Sitecore.Client.CoreDatabase.GetItem(
                    "/sitecore/content/Applications/PowerShell/PowerShellIse/Menus/Policies");
            if (menu != null && menu.HasChildren)
            {
                var queryString = WebUtil.GetQueryString("id");
                Options.AddFromDataSource(menu, queryString, new CommandContext(menu));
            }
        }

    }
}
