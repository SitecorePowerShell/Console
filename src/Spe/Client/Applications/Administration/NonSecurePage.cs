using Sitecore;
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

        private const string ErrorPageUrl = "/sitecore/admin/NonSecurePageDisabled.aspx";

        protected virtual bool IsEnabled
        {
            get
            {
                var environmentVariable = Environment.GetEnvironmentVariable("SITECORE_SPE_ADMIN_PAGE_ENABLED");
                if (!string.IsNullOrEmpty(environmentVariable))
                {
                    return MainUtil.GetBool(environmentVariable, false);
                }

                var enabledFile = base.Server.MapPath(EnabledFilePath);
                var disabledFile = base.Server.MapPath(DisabledFilePath);                
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
            base.Response.Redirect($"{ErrorPageUrl}?returnUrl={HttpUtility.UrlEncode(base.Request.Url.PathAndQuery)}");
        }

        protected override void OnInit(EventArgs args)
        {
            Assert.ArgumentNotNull(args, "arguments");
            this.CheckEnabled();
        }
    }
}