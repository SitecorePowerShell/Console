using System;
using System.Management.Automation;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using Cognifide.PowerShell.PowerShellIntegrations.Host;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Jobs;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Shell.Framework;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Button = Sitecore.Web.UI.HtmlControls.Button;
using Label = Sitecore.Web.UI.HtmlControls.Label;
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
            Result.Text = (string) HttpContext.Current.Session[sid];
            HttpContext.Current.Session.Remove(sid);
        }

    }
}