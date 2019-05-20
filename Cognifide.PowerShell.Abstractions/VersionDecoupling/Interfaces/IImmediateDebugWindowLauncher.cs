using Sitecore.Text;

namespace Cognifide.PowerShell.Abstractions.VersionDecoupling.Interfaces
{
    public interface IImmediateDebugWindowLauncher
    {
        void ShowImmediateWindow(UrlString url);
    }
}
