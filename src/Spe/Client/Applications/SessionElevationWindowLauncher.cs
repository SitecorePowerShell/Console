using Sitecore.Text;
using Sitecore.Web.UI.Sheer;
using Spe.Abstractions.VersionDecoupling.Interfaces;

namespace Spe.Client.Applications
{
    public class SessionElevationWindowLauncher : ISessionElevationWindowLauncher
    {
        public void ShowSessionElevationWindow(UrlString url)
        {
            var options = new ModalDialogOptions(url.ToString())
            {
                Header = string.Empty,
                Resizable = false,
                Width = "450",
                Height = "400",
                Response = true,
                Maximizable = false,
                Closable = false,
                Message = string.Empty
            };
            SheerResponse.ShowModalDialog(options);
        }
    }

}
