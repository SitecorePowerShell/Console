using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using Sitecore.Configuration;

namespace Cognifide.PowerShell.Core.VersionDecoupling
{
    public class VersionResolver
    {
        public static Version SitecoreVersionCurrent = GetVersionNumber();
        public static Version SitecoreVersion71 = new Version(7, 1);
        public static Version SitecoreVersion72 = new Version(7, 2);
        public static Version SitecoreVersion75 = new Version(7, 5);
        public static Version SitecoreVersion80 = new Version(8, 0);

        public static Version GetVersionNumber()
        {
            Version version;
            if (Version.TryParse(About.Version, out version))
            {
                return version;
            }
            if (Version.TryParse(About.GetVersionNumber(false), out version))
            {
                return version;
            }
            var kernel = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName()
                    .Name.Equals("Sitecore.Kernel", StringComparison.OrdinalIgnoreCase));

            if (kernel != null)
            {
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(kernel.Location);
                if (!Version.TryParse(fvi.FileVersion, out version))
                {
                    version = new Version(7, 0);
                }
            }
            else
            {
                version = new Version(7, 0);
            }
            return version;
        }

        public static bool IsVersionHigherOrEqual(Version version)
        {
            return version <= SitecoreVersionCurrent;
        }
    }
}