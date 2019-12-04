using Sitecore.Web;
using Spe.Abstractions.VersionDecoupling.Interfaces;

namespace Spe.Utility
{
    class UrlHandleWrapper : IUrlHandleWrapper
    {
        public bool TryGetHandle(out UrlHandle handle)
        {
            return UrlHandle.TryGetHandle(out handle);
        }

        public bool DisposeHandle(UrlHandle handle)
        {
            return UrlHandle.DisposeHandle(handle);
        }
    }
}
