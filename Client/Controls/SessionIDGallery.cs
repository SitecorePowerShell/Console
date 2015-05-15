using System;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Settings;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Resources;
using Sitecore.Shell.Applications.ContentManager.Galleries;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web;
using Sitecore.Web.UI;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.XmlControls;

namespace Cognifide.PowerShell.Client.Controls
{
    public class SessionIDGallery : GalleryForm
    {
        protected GalleryMenu Options;
        protected Scrollbox Sessions;
        private string CurrenSession { get; set; }

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
                CurrenSession = WebUtil.GetQueryString("currentSessionId");

                var sessions = ScriptSessionManager.GetAll();
                foreach (var session in sessions)
                {
                    var control = ControlFactory.GetControl("SessionIDGallery.Option") as XmlControl;
                    Assert.IsNotNull(control, typeof (XmlControl), "Xml Control \"{0}\" not found",
                        "Gallery.Versions.Option");
                    Context.ClientPage.AddControl(Sessions, control);

                    var icon = session.ApplianceType == ApplicationNames.AjaxConsole
                        ? "powershell/16x16/console.png"
                        : "powershell/16x16/PowerShell_Runner.png";
                    var builder = new ImageBuilder
                    {
                        Src = Images.GetThemedImageSource(icon, ImageDimension.id16x16),
                        Class = "scRibbonToolbarSmallGalleryButtonIcon",
                        Alt = session.ApplianceType
                    };
                    var type = builder.ToString();
                    if (session.ID == CurrenSession)
                    {
                        type = "<div class=\"versionNumSelected\">" + type + "</div>";
                    }
                    else
                    {
                        type = "<div class=\"versionNum\">" + type + "</div>";
                    }

                    control["Number"] = type;
                    control["SessionId"] = Translate.Text("ID: <b>{0}</b>", session.ID);
                    control["Location"] = Translate.Text("Location: <b>{0}</b>.", session.CurrentLocation);
                    control["UserName"] = Translate.Text("User: <b>{0}</b>.", session.UserName);
                    control["Click"] = string.Format("ise:setsessionid(id={0})", session.ID);
                }
                var item =
                    Sitecore.Client.CoreDatabase.GetItem(
                        "/sitecore/content/Applications/PowerShell/PowerShellIse/Menus/Sessions");
                if ((item != null) && item.HasChildren)
                {
                    var queryString = WebUtil.GetQueryString("id");
                    Options.AddFromDataSource(item, queryString, new CommandContext(item));
                }
            }
        }
    }
}