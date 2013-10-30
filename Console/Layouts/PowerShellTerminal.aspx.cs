using System;
using System.Web.UI;
using Cognifide.PowerShell.PowerShellIntegrations.Host;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;

namespace Cognifide.PowerShell.Console.Layouts
{
    public partial class PowerShellTerminal : Page
    {
        protected override void OnPreInit(EventArgs e)
        {
            Response.AddHeader("X-UA-Compatible", "IE=9");
            ApplicationSettings settings = ApplicationSettings.GetInstance(ApplicationNames.AjaxConsole, false);
            ForegroundColor = OutputLine.ProcessHtmlColor(settings.ForegroundColor);
            BackgroundColor = OutputLine.ProcessHtmlColor(settings.BackgroundColor);
            base.OnPreInit(e);
        }

        protected string ForegroundColor { get; set; }
        protected string BackgroundColor { get; set; }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }
    }
}