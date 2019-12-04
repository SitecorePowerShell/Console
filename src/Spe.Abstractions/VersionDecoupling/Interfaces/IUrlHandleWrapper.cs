using Sitecore.Web;

namespace Spe.Abstractions.VersionDecoupling.Interfaces
{
    public interface IUrlHandleWrapper
    {
        bool TryGetHandle(out UrlHandle handle);
        bool DisposeHandle(UrlHandle handle);
    }
}