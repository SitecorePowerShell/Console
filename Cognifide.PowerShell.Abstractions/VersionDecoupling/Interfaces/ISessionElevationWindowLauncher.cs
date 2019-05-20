using Sitecore.Text;

namespace Cognifide.PowerShell.Abstractions.VersionDecoupling.Interfaces
{
    public interface ISessionElevationWindowLauncher
    {
        void ShowSessionElevationWindow(UrlString url);
    }
}
