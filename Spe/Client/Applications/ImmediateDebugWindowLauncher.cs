using Sitecore.Text;
using Sitecore.Web.UI.Sheer;
using Spe.Abstractions.VersionDecoupling.Interfaces;

namespace Spe.Client.Applications
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
