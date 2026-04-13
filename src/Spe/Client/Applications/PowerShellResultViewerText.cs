using System;
using System.Web;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Spe.Core.Host;
using Spe.Core.Settings;

namespace Spe.Client.Applications
{
    public class PowerShellResultViewerText : BaseForm
    {
        protected Border Result;
        protected Scrollbox All;
        protected Scrollbox Promo;
        protected Literal DialogHeader;
        protected Literal PsProgressStatus;
        protected ThemedImage Icon;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            var sid = WebUtil.SafeEncode(WebUtil.GetQueryString("sid"));
            var title = WebUtil.GetQueryString("title");
            var icon = WebUtil.GetQueryString("icon");
            if (!string.IsNullOrEmpty(title))
            {
                DialogHeader.Text = title;
            }
            if (!string.IsNullOrEmpty(icon))
            {
                Icon.Src = icon;
            }
            var description = WebUtil.GetQueryString("desc");
            if (!string.IsNullOrEmpty(description))
            {
                PsProgressStatus.Text = description;
            }
            var settings = ApplicationSettings.GetInstance(ApplicationNames.Context, false);
            if (!Enum.TryParse(WebUtil.GetQueryString("fc"), true, out ConsoleColor fc))
            {
                fc = settings.ForegroundColor;
            }
            if (!Enum.TryParse(WebUtil.GetQueryString("bc"), true, out ConsoleColor bc))
            {
                bc = settings.BackgroundColor;
            }
            var foregroundColor = OutputLine.ProcessHtmlColor(fc);
            var backgroundColor = OutputLine.ProcessHtmlColor(bc);
            Result.Style.Add("color", foregroundColor);
            Result.Style.Add("background-color", backgroundColor);
            Result.Style.Add("font-family", settings.FontFamilyStyle);
            Result.Style.Add("font-size", $"{settings.FontSize}px");
            Result.InnerHtml = HttpContext.Current.Session[sid] as string ?? string.Empty;
            All.Style.Add("color", foregroundColor);
            All.Style.Add("background-color", backgroundColor);
            Promo.Style.Add("background-color", backgroundColor);            
            HttpContext.Current.Session.Remove(sid);
            SheerResponse.SetDialogValue(sid);
        }
    }
}