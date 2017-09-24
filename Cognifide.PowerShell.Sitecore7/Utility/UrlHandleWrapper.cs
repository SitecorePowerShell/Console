using Cognifide.PowerShell.Core.VersionDecoupling.Interfaces;
using Sitecore.Web;

namespace Cognifide.PowerShell.VersionSpecific.Utility
{
    class UrlHandleWrapper : IUrlHandleWrapper
    {
        public bool TryGetHandle(out UrlHandle handle)
        {
            try
            {
                handle = UrlHandle.Get();
            }
            catch
            {
                handle = null;
                return false;
            }
            return true;
        }

        public bool DisposeHandle(UrlHandle handle)
        {
            if (string.IsNullOrEmpty(handle.Handle) || WebUtil.GetSessionValue(handle.Handle) == null)
                return false;
            WebUtil.RemoveSessionValue(handle.Handle);
            return true;
        }
    }
}
