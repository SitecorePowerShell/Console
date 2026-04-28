# Calling SPE Remoting Without the Module

The `SPE` PowerShell module is the supported client. It handles JWT minting,
session lifecycle, retries, CliXml round-tripping, and policy header
inspection so the caller does not have to. But the remoting endpoint is just
an HTTP handler, and any client that can sign a JWT (or fetch one from an
IdP) and POST a body can drive it. This page is the "what is actually on the
wire" reference for callers that cannot or will not use the module: CI
pipelines on lean images, build agents, ops scripts, integration tests,
language ecosystems where no module exists.

The examples cover the round trip only: get a token, POST a script, parse
the response. Persistent sessions, file upload, structured errors, CliXml
argument passing, and the long-poll wait route are real but out of scope
here. Pointers to where each lives are at the bottom.

## Endpoint shape

```
POST https://<cm-host>/-/script/script/?sessionId=<guid>
                                       &outputFormat=json
                                       &persistentSession=false

Authorization: Bearer <jwt>
Content-Type:  text/plain

<powershell-script-text>
```

Notes that matter:

- **Path is fixed.** `/-/script/script/` is the script execution route. The
  trailing `script/` segment selects the service; do not drop it.
- **`sessionId` is a client-generated GUID.** Generate one per call when
  `persistentSession=false`. Reuse the same value across calls when
  `persistentSession=true` (the server keeps the runspace alive between
  calls; first caller wins ownership).
- **`outputFormat=json`** is what this page recommends and assumes. The
  server emits `Content-Type: application/json` and an
  `{ "output": [...], "errors": [...] }` envelope. `raw` returns plain
  text (good for one-line scripts that return a string). `clixml` is the
  default but is PowerShell-specific and is intended for module callers.
- **Body is the script text, UTF-8, no envelope.** Optional CliXml argument
  block can be appended after a `<#<sessionId>#>` delimiter (out of scope
  here; see "What else exists").
- **Status codes:** 200 on success, 424 when the script threw (the JSON
  body still parses and contains the captured output and errors), 401 on
  auth failure (with `X-SPE-AuthFailureReason` and `WWW-Authenticate`
  response headers identifying the reason).

## Authentication: shared-secret HS256

The default authentication mode. The shared secret is configured server-side
in `Spe.config` under
`<powershell><authenticationProvider><SharedSecret>` and is **never sent**
on the wire. The client mints an HS256-signed JWT locally; the server
verifies the signature with the same secret.

### Required claims

| Claim   | Value                                                                                  |
| ------- | -------------------------------------------------------------------------------------- |
| `iss`   | Must match an entry in `<allowedIssuers>` (default: `SPE Remoting`).                   |
| `aud`   | Must match an entry in `<allowedAudiences>` (default: the connection URI).             |
| `name`  | The Sitecore user the script will run as, e.g. `sitecore\admin`.                       |
| `exp`   | Unix-seconds expiration. Keep token lifetime short; default cap is 60 seconds.         |
| `iat`   | Optional but recommended; required if your config sets `MaxTokenLifetimeSeconds`.      |
| `nbf`   | Optional.                                                                              |
| `jti`   | Optional. Required if jti replay protection is enabled in OAuth mode (not HS256).      |

### Mint a token in PowerShell

```powershell
function New-SpeJwt {
    param(
        [Parameter(Mandatory)] [string] $Issuer,
        [Parameter(Mandatory)] [string] $Audience,
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [string] $SharedSecret,
        [int] $ValidForSeconds = 30
    )

    $now = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
    $header  = @{ alg = "HS256"; typ = "JWT" } | ConvertTo-Json -Compress
    $payload = @{
        iss  = $Issuer
        aud  = $Audience
        name = $Name
        iat  = $now
        exp  = $now + $ValidForSeconds
    } | ConvertTo-Json -Compress

    function ToBase64Url([byte[]] $bytes) {
        [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+','-').Replace('/','_')
    }

    $h = ToBase64Url ([Text.Encoding]::UTF8.GetBytes($header))
    $p = ToBase64Url ([Text.Encoding]::UTF8.GetBytes($payload))
    $signingInput = "$h.$p"

    $hmac = [System.Security.Cryptography.HMACSHA256]::new()
    $hmac.Key = [Text.Encoding]::UTF8.GetBytes($SharedSecret)
    $sig = ToBase64Url $hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($signingInput))

    "$signingInput.$sig"
}

$token = New-SpeJwt -Issuer "SPE Remoting" `
                    -Audience "https://spe.dev.local" `
                    -Name "sitecore\admin" `
                    -SharedSecret $env:SPE_SHARED_SECRET
```

### Mint a token in bash (curl users)

```bash
header_b64() { printf '%s' "$1" | base64 -w0 | tr '+/' '-_' | tr -d '='; }

now=$(date +%s); exp=$((now + 30))
header='{"alg":"HS256","typ":"JWT"}'
payload="{\"iss\":\"SPE Remoting\",\"aud\":\"https://spe.dev.local\",\"name\":\"sitecore\\\\admin\",\"iat\":$now,\"exp\":$exp}"

h=$(header_b64 "$header"); p=$(header_b64 "$payload")
sig=$(printf '%s' "$h.$p" | openssl dgst -sha256 -hmac "$SPE_SHARED_SECRET" -binary | base64 -w0 | tr '+/' '-_' | tr -d '=')

token="$h.$p.$sig"
```

## Authentication: OAuth bearer

Used for IdP-issued tokens (Sitecore Identity Server, Auth0, Entra ID,
Okta, Keycloak). The client never holds a signing key; it asks the IdP for
a token using `client_credentials` and presents it verbatim to SPE. The
server verifies the signature against the IdP's published JWKS, matches
the `(iss, client_id)` pair to an OAuth Client item in Sitecore, and
impersonates the user named on that item.

### Fetch a token

PowerShell:

```powershell
$response = Invoke-RestMethod `
    -Uri    "$IdentityHost/connect/token" `
    -Method POST `
    -ContentType "application/x-www-form-urlencoded" `
    -Body @{
        grant_type    = "client_credentials"
        client_id     = $ClientId
        client_secret = $ClientSecret
        scope         = "spe.remoting"
    }

$token = $response.access_token
```

curl:

```bash
token=$(curl -s -X POST "$IDENTITY_HOST/connect/token" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    --data-urlencode "grant_type=client_credentials" \
    --data-urlencode "client_id=$CLIENT_ID" \
    --data-urlencode "client_secret=$CLIENT_SECRET" \
    --data-urlencode "scope=spe.remoting" \
  | jq -r .access_token)
```

That same `$token` is used in the script POST below; the SPE side does not
care which mode minted it. See
[remoting-oauth-quickstart.md](remoting-oauth-quickstart.md) for the full
OAuth Client item setup.

## Round trip

Both modes share this step. Substitute whichever `$token` you produced.

### PowerShell

```powershell
$cmHost    = "https://spe.dev.local"
$sessionId = [guid]::NewGuid().ToString()
$script    = "Get-User -Current | Select-Object -ExpandProperty Name"

$response = Invoke-RestMethod `
    -Uri "$cmHost/-/script/script/?sessionId=$sessionId&outputFormat=json&persistentSession=false" `
    -Method POST `
    -Headers @{ Authorization = "Bearer $token" } `
    -Body $script `
    -ContentType "text/plain"

$response.output      # array of pipeline output values
$response.errors      # array of error strings (empty when the script succeeded)
```

### curl

```bash
session_id=$(uuidgen)
script='Get-User -Current | Select-Object -ExpandProperty Name'

curl -sS -X POST \
    "https://spe.dev.local/-/script/script/?sessionId=$session_id&outputFormat=json&persistentSession=false" \
    -H "Authorization: Bearer $token" \
    -H "Content-Type: text/plain" \
    --data-binary "$script" \
  | jq '.output, .errors'
```

## Response shape and status codes

The JSON body is the same envelope on success and on script failure:

```json
{
  "output": ["sitecore\\admin"],
  "errors": []
}
```

`output` carries whatever the script wrote to the pipeline, serialized via
`ConvertTo-Json -Depth 3`. `errors` is an array of strings by default. Add
`&errorFormat=structured` to receive each error as an object with
`message`, `errorCategory`, `categoryReason`, `fullyQualifiedErrorId`,
`exceptionType`, `scriptStackTrace`, and `invocationInfo` instead.

| Status | Meaning                                                           |
| ------ | ----------------------------------------------------------------- |
| 200    | Script ran. Body is the JSON envelope above.                      |
| 401    | Authentication failed. Check `X-SPE-AuthFailureReason` and `WWW-Authenticate` response headers for the specific reason. |
| 403    | Authentication succeeded but the policy denied the call (cmdlet not allowed, role missing, throttle exceeded). |
| 424    | The script threw. Body is still the JSON envelope; `errors` contains the captured error records. |
| 429    | Throttled. `Retry-After`, `X-RateLimit-Limit`, `X-RateLimit-Remaining`, and `X-RateLimit-Reset` headers say when to retry. |

## What else exists

Each of the following is real, in scope for the book, and intentionally
left out of this seed. Add chapters as needed.

- **Persistent sessions.** Reuse a `sessionId` and set
  `persistentSession=true` to keep variables and modules across calls.
  First caller claims the session; subsequent calls with a different
  identity are rejected. Clean up via the session-cleanup route.
- **CliXml argument passing.** Append `<#<sessionId>#><cliXmlArgs>` to the
  body. The server splits on the literal `<#<sessionId>#>` delimiter and
  deserializes the trailing block into a `$params` variable.
- **File upload and download.** `POST /-/script/file/<rootKey>/?path=...`
  and `GET /-/script/file/<rootKey>/?path=...` with bytes in the body.
  Path traversal is rejected with 403.
- **Media upload and download.** `/-/script/media/...` mirrors the file
  routes but writes into the media library.
- **CliXml and raw output.** `outputFormat=clixml` (the default when the
  parameter is omitted) returns a CliXml document that round-trips typed
  PowerShell objects. It is what the SPE module uses internally; for
  non-module callers it is rarely the right choice. `outputFormat=raw`
  returns plain text via `ToString()` on each pipeline value, no
  envelope, no errors structure - useful for scripts that emit a single
  string.
- **Stream capture.** Append `&captureStreams=true` to route Write-Verbose,
  Write-Warning, Write-Information, Write-Debug, and Write-Error into the
  output pipeline.
- **Long-poll wait.** `GET /-/script/?apiVersion=wait&jobId=...` blocks
  until a server-side job completes. Useful for jobs started by a prior
  script call.
- **Connection test.** `GET /-/script/?apiVersion=script` returns
  `{ SPEVersion, SitecoreVersion, CurrentTime }` for liveness checks.

For the policy and OAuth Client item that gates which cmdlets and which
impersonated user a token resolves to, see
[remoting-policy-setup.md](remoting-policy-setup.md) and
[remoting-oauth-quickstart.md](remoting-oauth-quickstart.md). For decoding
failure modes when a request comes back 401, see
[remoting-troubleshooting.md](remoting-troubleshooting.md).
