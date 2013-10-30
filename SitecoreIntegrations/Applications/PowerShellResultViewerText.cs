using System;
using System.Web;
using Cognifide.PowerShell.PowerShellIntegrations.Host;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
using Sitecore.Web;
using Sitecore.Web.UI.Sheer;
using Literal = Sitecore.Web.UI.HtmlControls.Literal;

namespace Cognifide.PowerShell.SitecoreIntegrations.Applications
{
    public class PowerShellResultViewerText : BaseForm
    {
        protected Literal Result;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            string sid = WebUtil.GetQueryString("sid");
            ApplicationSettings settings = ApplicationSettings.GetInstance(ApplicationNames.Context, false);
            var foregroundColor = OutputLine.ProcessHtmlColor(settings.ForegroundColor);
            var backgroundColor = OutputLine.ProcessHtmlColor(settings.BackgroundColor);
            Result.Style.Add("color", foregroundColor);
            Result.Style.Add("background-color", backgroundColor);
            Result.Text = HttpContext.Current.Session[sid] as string ?? string.Empty;
            HttpContext.Current.Session.Remove(sid);
        }

    }
}