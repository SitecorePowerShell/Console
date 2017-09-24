using Cognifide.PowerShell.Core.VersionDecoupling.Interfaces;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.VersionSpecific.Client.Applications
{
    public class SessionElevationWindowLauncher : ISessionElevationWindowLauncher
    {
        public void ShowSessionElevationWindow(UrlString url)
        {
            SheerResponse.ShowModalDialog(url.ToString(), "400", "350", string.Empty, true);
        }

    }
}
