using System;
using System.Web;
using System.Web.Security;
using Sitecore;
using Sitecore.Controls;
using Sitecore.Globalization;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Spe.Client.Controls;
using Spe.Core.Settings.Authorization;
using Button = Sitecore.Web.UI.HtmlControls.Button;
using Literal = Sitecore.Web.UI.HtmlControls.Literal;

namespace Spe.Client.Applications
{
    public class PowerShellSessionElevation : DialogPage
    {
        protected Button CancelButton;
        protected Border DataContextPanel;
        protected Literal CustomStyles;
        protected Button OKButton;
        protected Literal Result;
        protected Literal UserName;
        protected Literal PasswordLabel;
        protected PasswordExtended PasswordBox;
        protected Literal DialogHeader;
        protected Literal DialogDescription;
        protected Literal DialogMessage;
        protected Literal DialogMessageConfirm;

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
            DialogDescription.Text = Translate.Text(DialogDescription.Text, actionName);
            DialogMessage.Text = Translate.Text(DialogMessage.Text, actionName);
            DialogMessageConfirm.Text = Translate.Text(DialogMessageConfirm.Text, actionName);

            var tokenAction = SessionElevationManager.GetToken(AppName).Action;
            if (tokenAction == SessionElevationManager.TokenDefinition.ElevationAction.Confirm)
            {
                PasswordLabel.Visible = false;
                PasswordBox.Visible = false;
                DialogMessage.Visible = false;
                DialogMessageConfirm.Visible = true;
            }
        }

        protected void CancelClick()
        {
            SheerResponse.CloseWindow();
        }

        protected void OkClick()
        {
            var validateUser = true;
            var tokenAction = SessionElevationManager.GetToken(AppName).Action;
            if (tokenAction == SessionElevationManager.TokenDefinition.ElevationAction.Confirm)
            {
                validateUser = false;
            }

            if (!validateUser || Membership.ValidateUser(Sitecore.Context.User?.Name ?? string.Empty, PasswordBox.Value))
            {
	            SessionElevationManager.ElevateSessionToken(AppName);
	            SheerResponse.CloseWindow();
            }
            else
            {
	            SheerResponse.Alert(Texts.PowerShellSessionElevation_Could_not_validate);
            }
        }
    }
}