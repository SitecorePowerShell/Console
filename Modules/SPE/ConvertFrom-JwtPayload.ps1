function ConvertFrom-JwtPayload {
    <#
    .SYNOPSIS
        Decodes the payload (claims) segment of a JWT for inspection.

    .DESCRIPTION
        Base64url-decodes the second segment of a three-part JWT and parses the
        result as JSON. Returns the claims as a PSCustomObject, or the raw JSON
        string when -Raw is specified.

        IMPORTANT: this cmdlet is diagnostic only. It does NOT validate the
        token signature, expiration, issuer, audience, scopes, or any other
        claim. Do not use the decoded output as a basis for trust decisions.
        Signature verification is performed server-side by the SPE OAuth bearer
        provider against the configured JWKS.

        Typical uses:
        - Confirm that an access token carries the iss / aud / scope values
          your SPE config expects.
        - Diagnose 401 responses by comparing the token's actual claims to
          <allowedIssuers>, <allowedAudiences>, and <requiredScopes>.

    .PARAMETER Token
        A three-segment JWT string in the form "header.payload.signature".
        Accepts pipeline input. The -AccessToken alias matches the parameter
        name used by New-ScriptSession for consistency at the call site.

    .PARAMETER Raw
        Return the decoded JSON string instead of a parsed PSCustomObject.

    .EXAMPLE
        $token | ConvertFrom-JwtPayload

        Decode a JWT and return a PSCustomObject with its claims
        (iss, aud, exp, scope, ...).

    .EXAMPLE
        (ConvertFrom-JwtPayload -Token $token).aud

        Read a single claim. Returns a string or an array depending on how
        the issuer formatted `aud`.

    .EXAMPLE
        ConvertFrom-JwtPayload -Token $token -Raw

        Emit the decoded payload as a JSON string, e.g. for piping to another
        tool or writing to disk.
    #>
    [CmdletBinding()]
    [OutputType([pscustomobject], [string])]
    param(
        [Parameter(Mandatory = $true, Position = 0, ValueFromPipeline = $true)]
        [Alias('AccessToken')]
        [string]$Token,

        [switch]$Raw
    )

    process {
        if ([string]::IsNullOrWhiteSpace($Token)) {
            throw [System.ArgumentException]::new("Token cannot be null or empty.", 'Token')
        }

        $segments = $Token.Split('.')
        if ($segments.Length -ne 3) {
            throw [System.ArgumentException]::new(
                "Expected a 3-segment JWT (header.payload.signature); got $($segments.Length) segment(s).",
                'Token')
        }

        $segment = $segments[1]
        $pad = $segment.Length % 4
        if ($pad -eq 1) {
            throw [System.ArgumentException]::new("JWT payload segment has invalid base64url length.", 'Token')
        }
        if ($pad -gt 0) {
            $segment += '=' * (4 - $pad)
        }

        $b64 = $segment.Replace('-', '+').Replace('_', '/')
        try {
            $bytes = [Convert]::FromBase64String($b64)
        } catch {
            throw [System.ArgumentException]::new(
                "JWT payload segment is not valid base64url: $($_.Exception.Message)",
                'Token')
        }

        $json = [System.Text.Encoding]::UTF8.GetString($bytes)

        if ($Raw) {
            return $json
        }

        try {
            $json | ConvertFrom-Json
        } catch {
            throw [System.ArgumentException]::new(
                "JWT payload is not valid JSON: $($_.Exception.Message)",
                'Token')
        }
    }
}
