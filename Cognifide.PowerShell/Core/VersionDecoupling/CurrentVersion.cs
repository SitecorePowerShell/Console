using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using Cognifide.PowerShell.Commandlets;
using Sitecore.Configuration;

namespace Cognifide.PowerShell.Core.VersionDecoupling
{
    public static class CurrentVersion
    {
        public static Version SpeVersion => typeof(CurrentVersion).Assembly.GetName().Version;

        public static bool IsAtLeast(Version version)
        {
            return version <= SitecoreVersion.Current;
        }

        public static Version OrNewer(this Version requiredVersion, Action actionIfAtLeast)
        {
            return IsAtLeast(requiredVersion, actionIfAtLeast);
        }
        public static Version OrOlder(this Version requiredVersion, Action actionIfAtLeast)
        {
            return IsAtMost(requiredVersion, actionIfAtLeast);
        }


        public static Version IsAtLeast(Version requiredVersion, Action actionIfAtLeast)
        {
            if (requiredVersion <= SitecoreVersion.Current)
            {
                actionIfAtLeast();
                return null;
            }
            return requiredVersion;
        }
        public static Version IsAtMost(this Version requiredVersion, Action actionIfAtMost)
        {
            if (requiredVersion >= SitecoreVersion.Current)
            {
                actionIfAtMost();
                return null;
            }
            return requiredVersion;
        }
        public static Version Is(this Version requiredVersion, Action actionIfEquals)
        {
            if (requiredVersion == SitecoreVersion.Current)
            {
                actionIfEquals();
                return null;
            }
            return requiredVersion;
        }


        public static void Else(this Version requiredVersion, Action action)
        {
            if (requiredVersion != null)
            {
                action();
            }
        }

        public static void ElseWriteWarning(this Version version, BaseCommand command, string parameter, bool parameterScope)
        {
            if (version != null)
            {
                string unit = parameterScope ? "parameter" : "command";
                command.WriteWarning(
                    $"The \"{parameter}\" {unit} is not supported on this version of Sitecore due to platform limitations. This parameter is supported starting from Sitecore Version {version.Major}.{version.Minor}");
            }
        }

    }
}