using System;
using System.Linq;
using System.Web;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Client.Applications
{
    public class ConfirmChoice : BaseForm
    {
        protected Border Buttons;
        protected Literal DialogDescription;
        protected Literal DialogHeader;
        protected Literal Text;

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            if (Context.ClientPage.IsEvent)
                return;
            Text.Text = WebUtil.SafeEncode(WebUtil.GetQueryString("te")).Replace("\n", "<br/>");
            DialogHeader.Text = WebUtil.SafeEncode(WebUtil.GetQueryString("cp"));
            var defaultChoice = WebUtil.GetQueryString("dc");
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
                if (i.ToString() == defaultChoice)
                {
                    button.Class += " scButtonPrimary";
                    button.KeyCode = "13";
                }
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
    }
}