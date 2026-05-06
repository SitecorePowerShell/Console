using System;
using System.Web;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Spe.Core.Diagnostics;
using Spe.Core.Settings.Authorization;

namespace Spe.sitecore_modules.PowerShell.Services.Processors
{
    internal static class RemotingAuditFormat
    {
        // Audit field for the SSC item that authenticated this request. Returns
        // the SSC item GUID when one matched, or "<config>" when none did
        // (config-based shared secret, OAuth bearer without an SSC mapping,
        // or anonymous/basic auth). Emitting the GUID rather than the display
        // name keeps the value free of '=' / space delimiters used by the
        // structured-log parser (see Spe.Core.Diagnostics.LogSanitizer) and
        // stable across SSC item renames. Angle brackets distinguish the
        // sentinel from a real GUID.
        public static string FormatRemotingClientId(RemotingClient client)
            => client == null ? "<config>" : client.Id.ToString("D");

        // Audit field for the SSC AccessKeyId (the kid the JWT presented).
        // Stable across SSC item renames, so this is the value to use when
        // correlating audit lines back to a specific client item. Treats
        // null AccessKeyId (no client) and empty AccessKeyId (SSC items
        // with the field unset, e.g. OAuth-bearer-mapped clients) the same:
        // both render as "<none>" so the audit log never has a bare "kid=".
        public static string FormatRemotingClientKid(RemotingClient client)
            => string.IsNullOrEmpty(client?.AccessKeyId) ? "<none>" : client.AccessKeyId;

        // Audit field for the OAuth client_id claim. Populated by
        // OAuthBearerTokenAuthenticationProvider; null on SSC kid, configSecret,
        // basic, and session paths. Sanitized because the value originates in
        // an external IdP and may contain delimiter characters.
        public static string FormatOAuthClientId(TokenValidationResult tokenResult)
            => string.IsNullOrEmpty(tokenResult?.ClientId) ? "<none>" : LogSanitizer.SanitizeValue(tokenResult.ClientId);

        // Resolves the "authKind" audit field for the top-level
        // action=authenticated line. Bearer paths stash the value at
        // HttpContext.Items[RemotingHttpContextKeys.AuthKind]; non-bearer
        // paths are inferred from the Authorization header / query string.
        // One of:
        // sharedSecretClient | configSecret | oauthClient | basic | queryString | session.
        public static string ResolveAuthKindForAudit(HttpContext context, HttpRequest request)
        {
            var stashed = context?.Items[RemotingHttpContextKeys.AuthKind] as string;
            if (!string.IsNullOrEmpty(stashed)) return stashed;

            var authHeader = request?.Headers["Authorization"];
            if (!string.IsNullOrEmpty(authHeader) &&
                authHeader.StartsWith("Basic", StringComparison.OrdinalIgnoreCase))
            {
                return "basic";
            }

            if (!string.IsNullOrEmpty(request?.QueryString[AuthProcessor.ParamUser]) ||
                !string.IsNullOrEmpty(request?.QueryString[AuthProcessor.ParamPassword]))
            {
                return "queryString";
            }

            return "session";
        }

        // Audit-safe sessionId extracted from the request query string. The
        // top-of-handler audit lines (requestReceived, authenticated) fire
        // before per-request validation; running Guid.TryParse here means an
        // unauthenticated caller cannot inject arbitrary text into the audit
        // log via the sessionId param.
        public static string TryReadAuditSessionId(HttpRequest request)
        {
            var raw = request?.QueryString?["sessionId"];
            if (string.IsNullOrEmpty(raw)) return "<none>";
            return Guid.TryParse(raw, out var g) ? g.ToString("D") : "<invalid>";
        }
    }
}
