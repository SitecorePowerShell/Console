using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Text;

namespace Cognifide.PowerShell.Core.VersionDecoupling.Interfaces
{
    public interface IImmediateDebugWindowLauncher
    {
        void ShowImmediateWindow(UrlString url);
    }
}
