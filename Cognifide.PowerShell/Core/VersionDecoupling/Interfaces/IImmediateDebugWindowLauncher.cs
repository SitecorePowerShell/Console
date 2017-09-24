using Sitecore.Text;

namespace Cognifide.PowerShell.Core.VersionDecoupling.Interfaces
{
    public interface IImmediateDebugWindowLauncher
    {
        void ShowImmediateWindow(UrlString url);
    }
}
