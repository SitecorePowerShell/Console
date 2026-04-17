using System;
using System.Linq;
using System.Web;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Resources;
using Sitecore.Shell.Applications.ContentManager.Galleries;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web;
using Sitecore.Web.UI;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.XmlControls;
using Spe.Core.Host;
using Spe.Core.Settings;

namespace Spe.Client.Controls
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

                // Only surface sessions for the current user. ScriptSessionManager.GetAll
                // returns every session across the server, including other users' - showing
                // them here lets the user click-bind to a session they have no business with
                // and makes the "selected" highlight match multiple rows for any shared
                // persistent id (e.g. "ISE_Editing_Session").
                var currentUserName = Sitecore.Context.User?.Name;
                var sessions = ScriptSessionManager.GetAll()
                    .Where(s => string.Equals(s.UserName, currentUserName, StringComparison.OrdinalIgnoreCase));
                foreach (var session in sessions)
                {
                    var control = ControlFactory.GetControl("SessionIDGallery.Option") as XmlControl;
                    Assert.IsNotNull(control, typeof (XmlControl), "Xml Control \"{0}\" not found",
                        "Gallery.Versions.Option");
                    Context.ClientPage.AddControl(Sessions, control);

                    var icon = session.ApplianceType == ApplicationNames.Console
                        ? "powershell/16x16/console.png"
                        : "powershell/16x16/PowerShell_Runner.png";
                    var builder = new ImageBuilder
                    {
                        Src = Images.GetThemedImageSource(icon, ImageDimension.id16x16),
                        Class = "scRibbonToolbarSmallGalleryButtonIcon",
                        Alt = session.ApplianceType
                    };

                    var isSelected = session.ID == CurrenSession;
                    control["Number"] = builder.ToString();
                    control["SessionId"] = session.ID;
                    control["Location"] = session.CurrentLocation;
                    control["UserName"] = session.UserName;
                    control["Click"] = string.Format("ise:setsessionid(id={0})", session.ID);

                    // Outer row classes: keep scMenuPanelItem for the default
                    // hover/selection behavior from Gallery.css, add a speSessionRow
                    // hook for our flex layout, and mark the active row with
                    // speSessionRowSelected so it gets the "inspected variable" look.
                    control["RowClass"] = isSelected
                        ? "scMenuPanelItem speSessionRow speSessionRowSelected"
                        : "scMenuPanelItem speSessionRow";

                    // Kill button closes the session on click. Always render the
                    // slot (even for the active row) so the row content width -
                    // and therefore the pill position - is consistent across all
                    // rows; the active row just gets a non-interactive placeholder
                    // of the same dimensions. The user cannot delete the session
                    // they are currently bound to (which would leave the ribbon
                    // pointing at an id that no longer exists). stopPropagation
                    // keeps the row-level click from firing (which would otherwise
                    // re-select the session).
                    if (isSelected)
                    {
                        control["KillButton"] = "<span class=\"speSessionKillSlot\" aria-hidden=\"true\"></span>";
                    }
                    else
                    {
                        var escapedId = HttpUtility.JavaScriptStringEncode(session.ID);
                        control["KillButton"] =
                            $"<span class=\"speSessionKill\" title=\"Close session\" " +
                            $"onclick=\"event.stopPropagation(); " +
                            $"scForm.getParentForm().invoke('ise:killsessionid(id={escapedId})'); " +
                            $"var r=this.closest('.speSessionRow'); if(r){{r.remove();}} " +
                            $"return false;\">&#215;</span>";
                    }
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