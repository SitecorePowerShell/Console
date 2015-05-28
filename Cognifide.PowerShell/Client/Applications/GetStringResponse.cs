using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Client.Applications
{
    public class GetStringResponse : BaseForm
    {
        protected Border Buttons;
        protected string DefaultValue;
        protected Edit EditBox;
        protected string ErrorMessage;
        protected string MaxLength;
        protected Label Message;
        protected string ValidationExpression;

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            Message.Header = WebUtil.SafeEncode(WebUtil.GetQueryString("te"));
            Context.ClientPage.Title = WebUtil.SafeEncode(WebUtil.GetQueryString("cp"));
            DefaultValue = WebUtil.GetQueryString("dv");
            ValidationExpression = WebUtil.GetQueryString("vd");
            ErrorMessage = WebUtil.GetQueryString("em");
            MaxLength = WebUtil.GetQueryString("lm");

            if (Context.ClientPage.IsEvent)
                return;

            EditBox.Value = WebUtil.SafeEncode(DefaultValue);
            var i = 0;
            while (HttpContext.Current.Request.QueryString.AllKeys.Contains("btn_" + i))
            {
                var key = "btn_" + i;
                var button = new Button
                {
                    ID = key,
                    Header = HttpContext.Current.Request.QueryString[key],
                    Click = string.Format("button:click(value={0})", key)
                };
                Buttons.Controls.Add(button);
                i++;
            }
        }

        [HandleMessage("button:click", true)]
        protected void ButtonClick(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            Context.ClientPage.ClientResponse.SetDialogValue(args.Parameters["value"]);
            Context.ClientPage.ClientResponse.CloseWindow();
        }

        protected void OK()
        {
            var value = EditBox.Value;
            if (!string.IsNullOrEmpty(ValidationExpression))
            {
                if (!Regex.IsMatch(value, ValidationExpression))
                {
                    Context.ClientPage.ClientResponse.Alert(ErrorMessage);
                    return;
                }
            }
            Context.ClientPage.ClientResponse.SetDialogValue(EditBox.Value);
            Context.ClientPage.ClientResponse.CloseWindow();
        }

        protected void Cancel()
        {
            Context.ClientPage.ClientResponse.SetDialogValue(string.Empty);
            Context.ClientPage.ClientResponse.CloseWindow();
        }
    }
}