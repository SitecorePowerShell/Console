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
            SheerResponse.ShowModalDialog(url.ToString(), "450", "400", string.Empty, true);
        }

    }
}
