using Sitecore.Text;

namespace Spe.Abstractions.VersionDecoupling.Interfaces
{
    public interface ISessionElevationWindowLauncher
    {
        void ShowSessionElevationWindow(UrlString url);
    }
}