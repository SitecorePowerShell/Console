using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Xml;
using Cognifide.PowerShell.PowerShellIntegrations.Provider;
using Sitecore.Configuration;

namespace Cognifide.PowerShell.PowerShellIntegrations.Host
{
    [RunInstaller(true)]
    public class CognifideSitecorePowerShellSnapIn : CustomPSSnapIn
    {
        private static readonly List<CmdletConfigurationEntry> commandlets = new List<CmdletConfigurationEntry>();

        /// <summary>
        /// Create an instance of the CognifideSitecorePowerShellSnapIn class.
        /// </summary>
        public CognifideSitecorePowerShellSnapIn()
        {
        }

        static CognifideSitecorePowerShellSnapIn()
        {
            XmlNodeList cmdltsToIncludes = Factory.GetConfigNodes("powershell/commandlets/add");
            foreach (XmlElement cmdltToInclude in cmdltsToIncludes)
            {
                string[] cmdltTypeDef = cmdltToInclude.Attributes["type"].Value.Split(',');
                string cmdletType = cmdltTypeDef[0];
                string cmdletAssembly = cmdltTypeDef[1];
                WildcardPattern wildcard = GetWildcardPattern(cmdletType);
                Assembly assembly = Assembly.Load(cmdletAssembly);
                GetCommandletsFromAssembly(assembly, wildcard);
            }
        }

        private static void GetCommandletsFromAssembly(Assembly assembly, WildcardPattern wildcard)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (type.GetCustomAttributes(typeof(CmdletAttribute), true).Length > 0 &&
                    wildcard.IsMatch(type.FullName))
                {
                    var attribute = (CmdletAttribute)(type.GetCustomAttributes(typeof(CmdletAttribute), true)[0]);
                    Commandlets.Add(new CmdletConfigurationEntry(attribute.VerbName + "-" + attribute.NounName, type, "..\\Console\\Assets\\Cognifide.PowerShell-Help.xml"));
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


        /// <summary>
        /// Specify the name of the PowerShell snap-in.
        /// </summary>
        public override string Name
        {
            get
            {
                return "CognifideSitecorePowerShellSnapIn";
            }
        }

        /// <summary>
        /// Specify the vendor for the PowerShell snap-in.
        /// </summary>
        public override string Vendor
        {
            get
            {
                return "Cognifide";
            }
        }

        /// <summary>
        /// Specify the localization resource information for the vendor. 
        /// Use the format: resourceBaseName,VendorName. 
        /// </summary>
        public override string VendorResource
        {
            get
            {
                return "CognifideSitecorePowerShellSnapIn,Cognifide";
            }
        }

        /// <summary>
        /// Specify a description of the PowerShell snap-in.
        /// </summary>
        public override string Description
        {
            get
            {
                return "This snap-in integrates Sitecore & Powershell.";
            }
        }

        /// <summary>
        /// Specify the localization resource information for the description. 
        /// Use the format: resourceBaseName,Description. 
        /// </summary>
        public override string DescriptionResource
        {
            get
            {
                return "CognifideSitecorePowerShellSnapIn,This snap-in integrates Sitecore & Powershell.";
            }
        }

        /// <summary>
        /// Specify the cmdlets that belong to this custom PowerShell snap-in.
        /// </summary>
        public override Collection<CmdletConfigurationEntry> Cmdlets
        {
            get
            {
                return new Collection<CmdletConfigurationEntry>(commandlets);
            }
        }

        /// <summary>
        /// Specify the providers that belong to this custom PowerShell snap-in.
        /// </summary>
        private Collection<ProviderConfigurationEntry> _providers;
        public override Collection<ProviderConfigurationEntry> Providers
        {
            get
            {
                if (_providers == null)
                {
                    _providers = new Collection<ProviderConfigurationEntry>();
                    _providers.Add(new ProviderConfigurationEntry("Sitecore PowerShell Provider", typeof(PsSitecoreItemProvider), "..\\Console\\Assets\\Cognifide.PowerShell-Help.xml"));
                }

                return _providers;
            }
        }

        public static List<CmdletConfigurationEntry> Commandlets
        {
            get { return commandlets; }
        }
    }
}