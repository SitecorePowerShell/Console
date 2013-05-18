using System;
using System.Linq;
using System.Web;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.SitecoreIntegrations.Applications
{
    public class ConfirmChoice : BaseForm
    {
        protected Border Buttons;
        protected Literal Text;

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            if (Context.ClientPage.IsEvent)
                return;
            Text.Text = WebUtil.SafeEncode(WebUtil.GetQueryString("te"));
            string caption = WebUtil.SafeEncode(WebUtil.GetQueryString("cp"));
            //string defaultChoice = WebUtil.GetQueryString("dc");
            int i = 0;
            Context.ClientPage.Title = caption;
            while (HttpContext.Current.Request.QueryString.AllKeys.Contains("btn_" + i))
            {
                string key = "btn_" + i;
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
    }
}