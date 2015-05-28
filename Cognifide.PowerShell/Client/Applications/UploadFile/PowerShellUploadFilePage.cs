using System;
using System.Web.UI.HtmlControls;
using Sitecore.Shell.Web.UI;
using Sitecore.Web;

namespace Cognifide.PowerShell.Client.Applications.UploadFile
{
    public class PowerShellUploadFilePage : SecurePage
    {
        protected HtmlGenericControl Attach;

        private void InitializeComponent()
        {
            Load += Page_Load;
        }

        protected override void OnInit(EventArgs e)
        {
            Sitecore.Context.SetActiveSite("shell");
            InitializeComponent();
            base.OnInit(e);
        }

        private void Page_Load(object sender, EventArgs e)
        {
            Attach.Attributes["src"] = "/sitecore modules/Shell/PowerShell/UploadFile/PowerShellUploadFile2.aspx?" +
                                       WebUtil.GetQueryString();
        }
    }
}