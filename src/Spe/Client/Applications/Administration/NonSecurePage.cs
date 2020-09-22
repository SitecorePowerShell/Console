using Sitecore.Diagnostics;
using System;
using System.IO;
using System.Web;

namespace Spe.Client.Applications.Administration
{
    public class NonSecurePage : AdminPage
    {
        private const string DisabledFilePath = "~/sitecore/admin/disabled";

        private const string EnabledFilePath = "~/sitecore/admin/enabled";

        private const string SettingFilePrefix = "~/sitecore/admin/";

        private const string ErrorPageUrl = "/sitecore/admin/NonSecurePageDisabled.aspx";

        protected virtual bool IsEnabled
        {
            get
            {
                var enabledFile = base.Server.MapPath("~/sitecore/admin/enabled");
                var disabledFile = base.Server.MapPath("~/sitecore/admin/disabled");
                return !File.Exists(disabledFile) & File.Exists(enabledFile);
            }
        }

        protected virtual void CheckEnabled()
        {
            if (!this.IsEnabled)
            {
                this.HandleDisabled();
            }
        }

        protected virtual void HandleDisabled()
        {
            base.Response.Redirect(string.Concat("/sitecore/admin/NonSecurePageDisabled.aspx?returnUrl=", HttpUtility.UrlEncode(base.Request.Url.PathAndQuery)));
        }

        protected override void OnInit(EventArgs args)
        {
            Assert.ArgumentNotNull(args, "arguments");
            this.CheckEnabled();
        }
    }
}