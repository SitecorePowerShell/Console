using System;
using System.Web;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Settings;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Client.Applications
{
    public class PowerShellResultViewerText : BaseForm
    {
        protected Border Result;
        protected Scrollbox All;
        protected Scrollbox Promo;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            var sid = WebUtil.GetQueryString("sid");
            var settings = ApplicationSettings.GetInstance(ApplicationNames.Context, false);
            ConsoleColor fc;
            ConsoleColor bc;
            if (!Enum.TryParse(WebUtil.GetQueryString("fc"), true, out fc))
            {
                fc = settings.ForegroundColor;
            }
            if (!Enum.TryParse(WebUtil.GetQueryString("bc"), true, out bc))
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
        }
    }
}