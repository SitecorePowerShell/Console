using Sitecore.Text;

namespace Spe.Abstractions.VersionDecoupling.Interfaces
{
    public interface IImmediateDebugWindowLauncher
    {
        void ShowImmediateWindow(UrlString url);
    }
}