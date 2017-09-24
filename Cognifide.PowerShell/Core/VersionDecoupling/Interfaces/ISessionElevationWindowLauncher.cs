using Sitecore.Text;

namespace Cognifide.PowerShell.Core.VersionDecoupling.Interfaces
{
    public interface ISessionElevationWindowLauncher
    {
        void ShowSessionElevationWindow(UrlString url);
    }
}
