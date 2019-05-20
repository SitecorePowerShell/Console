using Cognifide.PowerShell.Abstractions.VersionDecoupling.Interfaces;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Client.Applications
{
    public class ImmediateDebugWindowLauncher : IImmediateDebugWindowLauncher
    {
        public void ShowImmediateWindow(UrlString url)
        {
            var options = new ModalDialogOptions(url.ToString())
            {
                Header = "Immediate Window",
                Resizable = true,
                Width = "800",
                Height = "600",
                Response = true
            };
            SheerResponse.ShowModalDialog(options);
        }
    }

}
