using System;
using System.Linq;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Settings;
using Sitecore;
using Sitecore.Collections;
using Sitecore.Data;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Resources;
using Sitecore.Security.Accounts;
using Sitecore.Shell.Applications.ContentManager.Galleries;
using Sitecore.Shell.Framework;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web;
using Sitecore.Web.UI;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.XmlControls;

namespace Cognifide.PowerShell.Client.Controls
{
    public class UserGallery : GalleryForm
    {
        protected GalleryMenu Options;
        protected Scrollbox Users;
        private string CurrentUser { get; set; }

        public override void HandleMessage(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            if (message.Name != "event:click")
            {
                Invoke(message, true);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            if (!Context.ClientPage.IsEvent)
            {
                CurrentUser = WebUtil.GetQueryString("currentUser");

                foreach (var user in new UserHistory().GetAccounts().Where(account => User.Exists(account)).Select(name => User.FromName(name,true)))
                {
                    AddUser(user);
                }
                var item =
                    Sitecore.Client.CoreDatabase.GetItem(
                        "/sitecore/content/Applications/PowerShell/PowerShellIse/Menus/Users");
                if ((item != null) && item.HasChildren)
                {
                    var queryString = WebUtil.GetQueryString("id");
                    var commandContext = new CommandContext(item);
                    commandContext.Parameters.Add("user", CurrentUser);
                    Options.AddFromDataSource(item, queryString, commandContext);
                }
            }
        }

        private void AddUser(User user)
        {
            var control = ControlFactory.GetControl("UserGallery.Option") as XmlControl;
            Assert.IsNotNull(control, "Xml Control \"UserGallery.Option\" not found");
            Context.ClientPage.AddControl(Users, control);

            var icon = user.Profile.Portrait;
            var builder = new ImageBuilder
            {
                Src = Images.GetThemedImageSource(icon, ImageDimension.id16x16),
                Class = "scRibbonToolbarSmallGalleryButtonIcon",
                Alt = user.Name
            };

            control["Class"] = (user.Name == CurrentUser) ? "selected" : string.Empty;
            control["UserIcon"] = $"<div class=\"versionNum\">{builder}</div>";
            control["Name"] = Translate.Text("<b>{0}</b>", user.Name);
            control["FullName"] = Translate.Text("<b>{0}</b>.",
                string.IsNullOrEmpty(user.Profile.FullName) ? user.LocalName : user.Profile.FullName);
            control["Click"] = $"ise:setuser(user={WebUtil.UrlEncode(user.Name).Replace(@"\", @"\\")})";
        }
    }
}