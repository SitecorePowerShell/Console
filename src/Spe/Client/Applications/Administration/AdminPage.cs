using System.Web;
using System.Web.UI;

namespace Spe.Client.Applications.Administration
{
    public class AdminPage : Page
    {
        private bool IsDeveloper => base.User.IsInRole("sitecore\\developer") || base.User.IsInRole("sitecore\\sitecore client developing");

        protected void CheckSecurity(bool isDeveloperAllowed)
        {
            if (Sitecore.Context.User.IsAdministrator) return;
            if (isDeveloperAllowed && this.IsDeveloper) return;
            var site = Sitecore.Context.Site;
            if (site != null)
            {
                base.Response.Redirect(
                    $"{site.LoginPage}?returnUrl={HttpUtility.UrlEncode(base.Request.Url.PathAndQuery)}");
            }
        }
    }
}