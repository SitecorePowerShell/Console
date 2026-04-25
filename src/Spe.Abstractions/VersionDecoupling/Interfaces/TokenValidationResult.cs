namespace Spe.Abstractions.VersionDecoupling.Interfaces
{
    public class TokenValidationResult
    {
        /// <summary>
        /// The client_session claim carried on HMAC-signed JWTs for session
        /// correlation. Populated by SharedSecretAuthenticationProvider only.
        /// </summary>
        public string ClientSessionId { get; set; }

        /// <summary>
        /// The iss claim from a validated bearer token. Populated by
        /// OAuthBearerTokenAuthenticationProvider so the handler can look up
        /// a matching OAuth Client item scoped to the issuing IdP.
        /// </summary>
        public string Issuer { get; set; }

        /// <summary>
        /// The client_id claim from a validated bearer token. Populated by
        /// OAuthBearerTokenAuthenticationProvider; combined with Issuer to
        /// scope OAuth Client item lookups.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Short tag describing why validation failed when an authentication
        /// provider sets it on rejection. Surfaces as the X-SPE-AuthFailureReason
        /// response header on 401s. Known values include "expired", "disabled",
        /// "invalid", "replay".
        /// </summary>
        public string FailureReason { get; set; }
    }
}
