using Sitecore.Web;

namespace Cognifide.PowerShell.Abstractions.VersionDecoupling.Interfaces
{
    public interface IUrlHandleWrapper
    {
        bool TryGetHandle(out UrlHandle handle);
        bool DisposeHandle(UrlHandle handle);
    }
}
