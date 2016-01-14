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
        protected Literal Result;
        protected Scrollbox All;
        protected Scrollbox Promo;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            var sid = WebUtil.GetQueryString("sid");
            var settings = ApplicationSettings.GetInstance(ApplicationNames.Context, false);
            var foregroundColor = OutputLine.ProcessHtmlColor(settings.ForegroundColor);
            var backgroundColor = OutputLine.ProcessHtmlColor(settings.BackgroundColor);
            Result.Style.Add("color", foregroundColor);
            Result.Style.Add("background-color", backgroundColor);
            Result.Style.Add("font-family", settings.FontFamily);
            Result.Style.Add("font-size", $"{settings.FontSize}px");
            Result.Text = HttpContext.Current.Session[sid] as string ?? string.Empty;
            All.Style.Add("color", foregroundColor);
            All.Style.Add("background-color", backgroundColor);
            Promo.Style.Add("background-color", backgroundColor);            
            HttpContext.Current.Session.Remove(sid);
        }
    }
}