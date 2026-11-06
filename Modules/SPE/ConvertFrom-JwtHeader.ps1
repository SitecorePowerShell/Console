function ConvertFrom-JwtHeader {
    <#
    .SYNOPSIS
        Decodes the header segment of a JWT for inspection.

    .DESCRIPTION
        Base64url-decodes the first segment of a three-part JWT and parses the
        result as JSON. Returns the header as a PSCustomObject, or the raw JSON
        string when -Raw is specified.

        IMPORTANT: this cmdlet is diagnostic only. It does NOT validate the
        token signature or any claim. The header advertises the signing
        algorithm (alg) and key id (kid) the issuer claims to have used; those
        values are only meaningful once a validator has actually verified the
        signature with the corresponding JWKS entry.

        Typical uses:
        - Confirm the signing algorithm matches what the SPE provider allows
          (e.g. RS256 vs HS256).
        - Read kid when diagnosing JWKS lookup failures.

    .PARAMETER Token
        A three-segment JWT string in the form "header.payload.signature".
        Accepts pipeline input. The -AccessToken alias matches the parameter
        name used by New-ScriptSession for consistency at the call site.

    .PARAMETER Raw
        Return the decoded JSON string instead of a parsed PSCustomObject.

    .EXAMPLE
        $token | ConvertFrom-JwtHeader

        Shows alg, typ, and (for asymmetric tokens) kid.

    .EXAMPLE
        (ConvertFrom-JwtHeader -Token $token).alg

        Read just the signing algorithm.
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

        $segment = $segments[0]
        $pad = $segment.Length % 4
        if ($pad -eq 1) {
            throw [System.ArgumentException]::new("JWT header segment has invalid base64url length.", 'Token')
        }
        if ($pad -gt 0) {
            $segment += '=' * (4 - $pad)
        }

        $b64 = $segment.Replace('-', '+').Replace('_', '/')
        try {
            $bytes = [Convert]::FromBase64String($b64)
        } catch {
            throw [System.ArgumentException]::new(
                "JWT header segment is not valid base64url: $($_.Exception.Message)",
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
                "JWT header is not valid JSON: $($_.Exception.Message)",
                'Token')
        }
    }
}
