using System;
using System.Collections.Generic;
using Spe.Abstractions.VersionDecoupling.Interfaces;

namespace Spe.Core.Settings.Authorization
{
    // Pure-function startup audit. Returns Warn-level findings for auth
    // provider configurations that route deterministically but probably
    // weren't what the operator intended. Never refuses to start; the
    // ServiceAuthenticationManager static ctor calls FindWarnings once
    // at app load and logs each entry at Warn level.
    //
    // Three findings:
    //   1. Two OAuthBearer providers sharing an (issuer, alg) tuple -
    //      first-match wins on token routing; operator likely meant to
    //      partition by issuer, not duplicate.
    //   2. An OAuthBearer issuer also appearing in a SharedSecret
    //      AllowedIssuers - alg dispatch keeps them separate at runtime,
    //      but it's almost always a copy-paste typo.
    //   3. OAuthBearer provider with RequiredScopes but empty
    //      AllowedAudiences - the token is anchored to a resource we
    //      never claim to be ours, suggesting incomplete config.
    public static class AuthProviderConfigValidator
    {
        public static IList<string> FindWarnings(IList<ISpeAuthenticationProvider> providers)
        {
            var warnings = new List<string>();
            if (providers == null || providers.Count == 0) return warnings;

            FindOAuthOverlap(providers, warnings);
            FindCrossProviderIssuerLeak(providers, warnings);
            FindScopesWithoutAudiences(providers, warnings);

            return warnings;
        }

        private static void FindOAuthOverlap(
            IList<ISpeAuthenticationProvider> providers, List<string> warnings)
        {
            var oauthProviders = new List<OAuthBearerTokenAuthenticationProvider>();
            foreach (var p in providers)
            {
                if (p is OAuthBearerTokenAuthenticationProvider oauth)
                {
                    oauthProviders.Add(oauth);
                }
            }

            for (int i = 0; i < oauthProviders.Count; i++)
            {
                var a = oauthProviders[i];
                if (a.AllowedIssuers == null || a.AllowedAlgorithms == null) continue;

                for (int j = i + 1; j < oauthProviders.Count; j++)
                {
                    var b = oauthProviders[j];
                    if (b.AllowedIssuers == null || b.AllowedAlgorithms == null) continue;

                    foreach (var issuerA in a.AllowedIssuers)
                    {
                        var canonA = JwtClaimValidator.CanonicalizeIssuer(issuerA);
                        foreach (var issuerB in b.AllowedIssuers)
                        {
                            if (!string.Equals(canonA, JwtClaimValidator.CanonicalizeIssuer(issuerB),
                                StringComparison.Ordinal)) continue;

                            foreach (var algA in a.AllowedAlgorithms)
                            {
                                foreach (var algB in b.AllowedAlgorithms)
                                {
                                    if (!string.Equals(algA, algB, StringComparison.Ordinal)) continue;
                                    warnings.Add(
                                        $"OAuthBearer providers share (issuer, alg)=({issuerA}, {algA}) - " +
                                        "first match wins on token routing; partition by issuer or remove the duplicate.");
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void FindCrossProviderIssuerLeak(
            IList<ISpeAuthenticationProvider> providers, List<string> warnings)
        {
            var sharedIssuers = new List<string>();
            foreach (var p in providers)
            {
                if (p is SharedSecretAuthenticationProvider shared && shared.AllowedIssuers != null)
                {
                    foreach (var i in shared.AllowedIssuers)
                    {
                        sharedIssuers.Add(i);
                    }
                }
            }
            if (sharedIssuers.Count == 0) return;

            foreach (var p in providers)
            {
                if (!(p is OAuthBearerTokenAuthenticationProvider oauth)) continue;
                if (oauth.AllowedIssuers == null) continue;
                foreach (var oauthIssuer in oauth.AllowedIssuers)
                {
                    var canonOauth = JwtClaimValidator.CanonicalizeIssuer(oauthIssuer);
                    foreach (var sharedIssuer in sharedIssuers)
                    {
                        if (!string.Equals(canonOauth,
                            JwtClaimValidator.CanonicalizeIssuer(sharedIssuer), StringComparison.Ordinal))
                            continue;
                        warnings.Add(
                            $"Issuer '{oauthIssuer}' is declared on both an OAuthBearer provider and a " +
                            "SharedSecret AllowedIssuers list - alg dispatch keeps them separate but this " +
                            "is almost always a copy-paste typo.");
                    }
                }
            }
        }

        private static void FindScopesWithoutAudiences(
            IList<ISpeAuthenticationProvider> providers, List<string> warnings)
        {
            foreach (var p in providers)
            {
                if (!(p is OAuthBearerTokenAuthenticationProvider oauth)) continue;
                var hasScopes = oauth.RequiredScopes != null && oauth.RequiredScopes.Count > 0;
                var hasAudiences = oauth.AllowedAudiences != null && oauth.AllowedAudiences.Count > 0;
                if (!hasScopes || hasAudiences) continue;

                var issuerHint = oauth.AllowedIssuers != null && oauth.AllowedIssuers.Count > 0
                    ? oauth.AllowedIssuers[0]
                    : "<unknown>";
                warnings.Add(
                    $"OAuthBearer provider for issuer '{issuerHint}' has RequiredScopes but no " +
                    "AllowedAudiences - tokens carry scopes for a resource SPE never claims as its own. " +
                    "Add an audience or drop the scope requirement.");
            }
        }
    }
}
