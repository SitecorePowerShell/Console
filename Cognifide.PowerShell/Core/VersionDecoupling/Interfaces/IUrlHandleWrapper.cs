using System;
using Sitecore.Web;

namespace Cognifide.PowerShell.Core.VersionDecoupling.Interfaces
{
    public interface IUrlHandleWrapper
    {
        bool TryGetHandle(out UrlHandle handle);
        bool DisposeHandle(UrlHandle handle);
    }
}
