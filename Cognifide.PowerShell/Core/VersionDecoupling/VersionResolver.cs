using System;
using Sitecore.Configuration;

namespace Cognifide.PowerShell.Core.VersionDecoupling
{
    public class VersionResolver
    {
        public static Version SitecoreVersionCurrent = GetVersionNumber(About.Version);
        public static Version SitecoreVersion72 = new Version(7, 2);
        public static Version SitecoreVersion75 = new Version(7, 5);
        public static Version SitecoreVersion80 = new Version(8, 0);

        public static bool IsVersionHigherOrEqual(Version version)
        {
            return version <= SitecoreVersionCurrent;
        }

        public static Version GetVersionNumber(string version)
        {
            Version result;
            if (Version.TryParse(version, out result))
            {
                return result;
            }

            return Version.TryParse(About.GetVersionNumber(false), out result) ? result : new Version(0, 0);
        }
    }
}