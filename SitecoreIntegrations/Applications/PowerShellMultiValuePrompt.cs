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
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using Button = Sitecore.Web.UI.HtmlControls.Button;
using Label = Sitecore.Web.UI.HtmlControls.Label;
using Literal = Sitecore.Web.UI.HtmlControls.Literal;

namespace Cognifide.PowerShell.SitecoreIntegrations.Applications
{
    public class PowerShellMultiValuePrompt : WizardForm
    {
        protected Literal Result;
        protected Button OkButton;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            string sid = WebUtil.GetQueryString("sid");
            var result = HttpContext.Current.Session[sid];
            //HttpContext.Current.Session.Remove(sid);

        }

        protected override void EndWizard()
        {
            string sid = WebUtil.GetQueryString("sid");
            SheerResponse.SetDialogValue(sid);
            SheerResponse.CloseWindow();
        }
    }
}