using Cognifide.PowerShell.Core.VersionDecoupling.Interfaces;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.VersionSpecific.Client.Applications
{
    public class ImmediateDebugWindowLauncher : IImmediateDebugWindowLauncher
    {
        public void ShowImmediateWindow(UrlString url)
        {
            SheerResponse.ShowModalDialog(url.ToString(), "800", "600", string.Empty, true);
        }

    }
}
