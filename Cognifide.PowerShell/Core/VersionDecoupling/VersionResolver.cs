using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Configuration;

namespace Cognifide.PowerShell.Core.VersionDecoupling
{
    public class VersionResolver
    {
        public static Version SitecoreVersionCurrent = Version.Parse(About.Version);
        public static Version SitecoreVersion72 = new Version(7, 2);
        public static Version SitecoreVersion75 = new Version(7, 5);
        public static Version SitecoreVersion80 = new Version(8, 0);

        public static bool IsVersionHigherOrEqual(Version version)
        {
            return version <= SitecoreVersionCurrent;
        }
    }
}