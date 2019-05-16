using Cognifide.PowerShell.Core.VersionDecoupling.Interfaces;
using Sitecore.Web;

namespace Cognifide.PowerShell.Utility
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
