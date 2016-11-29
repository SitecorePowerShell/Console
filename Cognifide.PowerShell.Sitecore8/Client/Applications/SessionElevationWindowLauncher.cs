using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cognifide.PowerShell.Core.VersionDecoupling.Interfaces;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.VersionSpecific.Client.Applications
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
