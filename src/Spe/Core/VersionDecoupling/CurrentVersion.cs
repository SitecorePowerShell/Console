using System;
using System.Linq;
using System.Reflection;
using Spe.Commands;

namespace Spe.Core.VersionDecoupling
{
    public static class CurrentVersion
    {
        public static Version SpeVersion => typeof(CurrentVersion).Assembly.GetName().Version;

        public static string SpeVersionFull =>
            typeof(CurrentVersion).Assembly.GetName().Version + (string.IsNullOrEmpty(BetaVersion)
                ? string.Empty
                : $" ({BetaVersion})");
        
        public static string BetaVersion  { get { return GetAssemblyMetadataAttribute("BetaVersion"); } }
        public static string CodeName  { get { return GetAssemblyMetadataAttribute("CodeName"); } }
        
        private static string GetAssemblyMetadataAttribute(string key)
        {
            AssemblyMetadataAttribute[] attributes = (AssemblyMetadataAttribute[])Attribute.GetCustomAttributes(Assembly.GetExecutingAssembly(), typeof(AssemblyMetadataAttribute));
            return attributes.Where(a => a.Key == key).Select(a => a.Value).FirstOrDefault() ?? string.Empty;
        }
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