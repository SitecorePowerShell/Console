using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Xml;
using Cognifide.PowerShell.Core.Diagnostics;
using Cognifide.PowerShell.Core.Provider;
using Cognifide.PowerShell.Core.Utility;
using Cognifide.PowerShell.Core.Validation;
using Sitecore.Configuration;
using Sitecore.ContentSearch.Utilities;
using Sitecore.Diagnostics;
using Sitecore.Pipelines;
using Pipeline = Sitecore.Pipelines.Pipeline;

namespace Cognifide.PowerShell.Core.Host
{
    [RunInstaller(true)]
    public class CognifideSitecorePowerShellSnapIn : CustomPSSnapIn
    {
        /// <summary>
        ///     Specify the providers that belong to this custom PowerShell snap-in.
        /// </summary>
        private Collection<ProviderConfigurationEntry> providers;

        private bool initialized;

        static CognifideSitecorePowerShellSnapIn()
        {
            var cmdltsToIncludes = Factory.GetConfigNodes("powershell/commandlets/add");
            foreach (XmlElement cmdltToInclude in cmdltsToIncludes)
            {
                var cmdltTypeDef = cmdltToInclude.Attributes["type"].Value.Split(',');
                var cmdletType = cmdltTypeDef[0];
                var cmdletAssembly = cmdltTypeDef[1];
                var wildcard = GetWildcardPattern(cmdletType);
                try
                {
                    var assembly = Assembly.Load(cmdletAssembly);
                    GetCommandletsFromAssembly(assembly, wildcard);
                }
                catch (Exception ex)
                {
                    var typeLoadException = ex as ReflectionTypeLoadException;
                    if (typeLoadException != null)
                    {
                        var loaderExceptions = typeLoadException.LoaderExceptions;
                        var message = loaderExceptions.Aggregate(string.Empty, (current, exc) => current + exc.Message);
                        PowerShellLog.Error($"Error while loading commandlets list: {message}", typeLoadException);
                    }
                }
            }
        }

        /// <summary>
        ///     Specify the name of the PowerShell snap-in.
        /// </summary>
        public override string Name => "CognifideSitecorePowerShellSnapIn";

        /// <summary>
        ///     Specify the vendor for the PowerShell snap-in.
        /// </summary>
        public override string Vendor => "Cognifide";

        /// <summary>
        ///     Specify the localization resource information for the vendor.
        ///     Use the format: resourceBaseName,VendorName.
        /// </summary>
        public override string VendorResource => "CognifideSitecorePowerShellSnapIn,Cognifide";

        /// <summary>
        ///     Specify a description of the PowerShell snap-in.
        /// </summary>
        public override string Description => "This snap-in integrates Sitecore & Powershell.";

        /// <summary>
        ///     Specify the localization resource information for the description.
        ///     Use the format: resourceBaseName,Description.
        /// </summary>
        public override string DescriptionResource => "CognifideSitecorePowerShellSnapIn,This snap-in integrates Sitecore & Powershell.";

        /// <summary>
        ///     Specify the cmdlets that belong to this custom PowerShell snap-in.
        /// </summary>
        public override Collection<CmdletConfigurationEntry> Cmdlets => new Collection<CmdletConfigurationEntry>(Commandlets);
        public static List<CmdletConfigurationEntry> Commandlets { get; } = new List<CmdletConfigurationEntry>();
        public static List<SessionStateCommandEntry> SessionStateCommandlets { get; } = new List<SessionStateCommandEntry>();

        public static Dictionary<string, string> Completers { get; } = new Dictionary<string, string>();

        public override Collection<ProviderConfigurationEntry> Providers
        {
            get
            {
                if (!initialized)
                {
                    Initialize();
                }
                return providers ?? (providers = new Collection<ProviderConfigurationEntry>
                {
                    new ProviderConfigurationEntry("Sitecore PowerShell Provider",
                        typeof (PsSitecoreItemProvider),
                        @"..\sitecore modules\PowerShell\Assets\Cognifide.PowerShell.dll-Help.maml")
                });
            }
        }

        private static void GetCommandletsFromAssembly(Assembly assembly, WildcardPattern wildcard)
        {
            var helpPath = Path.GetDirectoryName(AppDomain.CurrentDomain.SetupInformation.PrivateBinPath) +
                           @"\sitecore modules\PowerShell\Assets\Cognifide.PowerShell.dll-Help.maml";
            foreach (var type in assembly.GetTypes())
            {
                if (type.GetCustomAttributes(typeof (CmdletAttribute), true).Length > 0 &&
                    wildcard.IsMatch(type.FullName))
                {
                    var attribute = (CmdletAttribute) (type.GetCustomAttributes(typeof (CmdletAttribute), true)[0]);
                    Commandlets.Add(new CmdletConfigurationEntry(attribute.VerbName + "-" + attribute.NounName, type,
                        helpPath));
                    SessionStateCommandlets.Add(new SessionStateCmdletEntry(attribute.VerbName + "-" + attribute.NounName, type, helpPath));
                    foreach (var property in type.GetProperties())
                    {
                        var propAttribute = (AutocompleteSetAttribute)
                            property.GetCustomAttributes(typeof (AutocompleteSetAttribute), true).FirstOrDefault();
                        if (propAttribute != null)
                        {
                            Completers.Add(attribute.VerbName + "-" + attribute.NounName+":"+property.Name, 
                                "["+type.FullName+"]::"+propAttribute.Values);
                        }
                    }
                }
            }
        }

        protected static WildcardPattern GetWildcardPattern(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                name = "*";
            }
            const WildcardOptions options = WildcardOptions.IgnoreCase | WildcardOptions.Compiled;
            var wildcard = new WildcardPattern(name, options);
            return wildcard;
        }

        private void Initialize()
        {
            initialized = true;
            Pipeline.Start("initialize", new PipelineArgs(), true);
        }
    }
}