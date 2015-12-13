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
