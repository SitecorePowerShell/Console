using System.Collections.Generic;
using System.Xml;
using Sitecore.Configuration;
using Spe.Abstractions.VersionDecoupling.Interfaces;

namespace Spe.Core.Settings.Authorization
{
    /// <summary>
    /// Loads the configured authentication provider(s) from Spe.config at
    /// static init time. Each incoming remoting request is validated by one
    /// of the providers - the handler picks which based on the JWT header
    /// algorithm.
    ///
    /// Two config paths are consulted:
    /// <list type="bullet">
    ///   <item><description>
    ///     <c>/sitecore/powershell/authenticationProvider</c> (singular) -
    ///     the SharedSecret provider. Always registered. Existing operator
    ///     patches to this element (sharedSecret, allowedIssuers,
    ///     allowedAudiences, etc.) apply unchanged.
    ///   </description></item>
    ///   <item><description>
    ///     <c>/sitecore/powershell/authenticationProviders/*</c> (plural) -
    ///     additional bearer-token validators. Empty by default; populated
    ///     by Spe.OAuthBearer.config when that file is activated.
    ///   </description></item>
    /// </list>
    /// </summary>
    public static class ServiceAuthenticationManager
    {
        static ServiceAuthenticationManager()
        {
            var providers = new List<ISpeAuthenticationProvider>();

            var primary = Factory.CreateObject(
                "/sitecore/powershell/authenticationProvider", assert: false)
                as ISpeAuthenticationProvider;
            if (primary != null)
            {
                providers.Add(primary);
            }

            foreach (XmlNode node in Factory.GetConfigNodes("powershell/authenticationProviders/*"))
            {
                var provider = Factory.CreateObject(node, assert: false)
                    as ISpeAuthenticationProvider;
                if (provider != null)
                {
                    providers.Add(provider);
                }
            }

            AuthenticationProviders = providers;
            AuthenticationProvider = primary;
        }

        /// <summary>
        /// The primary (singular) authentication provider. When the standard
        /// Spe.config is loaded this is the SharedSecret provider. Retained
        /// as a back-compat alias; new code should use
        /// <see cref="AuthenticationProviders"/> or the handler's algorithm-
        /// based dispatch to reach the right provider for a given token.
        /// </summary>
        public static ISpeAuthenticationProvider AuthenticationProvider { get; }

        /// <summary>
        /// All registered authentication providers, in config-load order.
        /// The SharedSecret provider (if configured) is always first; any
        /// bearer-token providers registered under
        /// <c>/sitecore/powershell/authenticationProviders</c> follow.
        /// </summary>
        public static IReadOnlyList<ISpeAuthenticationProvider> AuthenticationProviders { get; }
    }
}
