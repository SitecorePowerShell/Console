using System;
using System.Web;
using System.Web.Security;
using Cognifide.PowerShell.Client.Controls;
using Cognifide.PowerShell.Core.Settings.Authorization;
using Sitecore;
using Sitecore.Controls;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Button = Sitecore.Web.UI.HtmlControls.Button;
using Literal = Sitecore.Web.UI.HtmlControls.Literal;

namespace Cognifide.PowerShell.Client.Applications
{
    public class PowerShellSessionElevation : DialogPage
    {
        protected Button CancelButton;
        protected Border DataContextPanel;
        protected Literal CustomStyles;
        protected Button OKButton;
        protected Literal Result;
        protected Literal UserName;
        protected PasswordExtended PasswordBox;
        protected Literal DialogHeader;
        protected Literal DialogDescription;
        protected Literal DialogMessage;

        protected string AppName
        {
            get { return StringUtil.GetString(Sitecore.Context.ClientPage.ServerProperties["AppName"]); }
            set { Sitecore.Context.ClientPage.ServerProperties["AppName"] = value ?? string.Empty; }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (Sitecore.Context.ClientPage.IsEvent)
                return;
            AppName = WebUtil.GetQueryString("app");
            var actionName = WebUtil.GetQueryString("action");
            if (string.IsNullOrEmpty(actionName))
            {
                actionName = SessionElevationManager.ExecuteAction;
            }
            UserName.Text = Sitecore.Context.User?.Name ?? string.Empty;
            HttpContext.Current.Response.AddHeader("X-UA-Compatible", "IE=edge");
            DialogDescription.Text = String.Format(DialogDescription.Text, actionName);
            DialogMessage.Text = String.Format(DialogMessage.Text, actionName);
        }

        protected void CancelClick()
        {
            SheerResponse.CloseWindow();
        }

        protected void OkClick()
        {
            if (Membership.ValidateUser(Sitecore.Context.User?.Name ?? string.Empty, PasswordBox.Value))
            {
                SessionElevationManager.ElevateSessionToken(AppName);
                SheerResponse.CloseWindow();
            }
        }
    }
}