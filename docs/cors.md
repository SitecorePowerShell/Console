# CORS Configuration

Sitecore PowerShell Extensions 9.0+ supports Cross-Origin Resource Sharing (CORS)
for browser-based API calls to SPE web services.

## Quick Start

Add a `<cors>` element inside the service you want to enable in your Sitecore config patch:

```xml
<configuration xmlns:patch="http://www.sitecore.net/xmlconfig/">
  <sitecore>
    <powershell>
      <services>
        <restfulv2>
          <cors allowedOrigins="https://myapp.example.com" />
        </restfulv2>
      </services>
    </powershell>
  </sitecore>
</configuration>
```

This allows cross-origin requests from `https://myapp.example.com` to the RESTful v2
API endpoint (`/-/script/v2/`).

## Configuration Reference

### Attributes

| Attribute | Required | Default | Description |
|---|---|---|---|
| `allowedOrigins` | Yes | -- | Origins to allow. Use `*` for any origin, or a pipe-delimited list of specific origins (e.g. `https://a.com\|https://b.com`). |
| `allowCredentials` | No | `false` | When `true`, adds `Access-Control-Allow-Credentials: true` to responses. Cannot be combined with wildcard origins -- the server will log a warning and disable credentials if both are set. |
| `maxAge` | No | `3600` | Value for `Access-Control-Max-Age` header (seconds). Tells browsers how long to cache the preflight response. |

### Supported Services

| Service | URL Pattern | Config Element |
|---|---|---|
| RESTful v2 | `/-/script/v2/{db}/{path}` | `<restfulv2>` |
| Remoting | `/-/script/script/` | `<remoting>` |
| File Download | `/-/script/file/{root}/` | `<fileDownload>` |
| Media Download | `/-/script/media/{db}/` | `<mediaDownload>` |
| Handle Download | `/-/script/handle/` | `<handleDownload>` |

Each service is configured independently. A service without a `<cors>` element will
not include any CORS headers in its responses.

## Examples

### Development (wildcard)

Allow any origin -- suitable for local development only:

```xml
<restfulv2>
  <patch:attribute name="enabled">true</patch:attribute>
  <cors allowedOrigins="*" />
</restfulv2>
```

### Production (specific origins)

Allow only your application origins:

```xml
<restfulv2>
  <patch:attribute name="enabled">true</patch:attribute>
  <cors allowedOrigins="https://app.example.com|https://admin.example.com"
        allowCredentials="true" maxAge="7200" />
</restfulv2>
```

### Multiple services

Enable CORS on both remoting and RESTful v2:

```xml
<powershell>
  <services>
    <restfulv2>
      <cors allowedOrigins="https://app.example.com" />
    </restfulv2>
    <remoting>
      <cors allowedOrigins="https://app.example.com" allowCredentials="true" />
    </remoting>
  </services>
</powershell>
```

## How It Works

SPE handles CORS in two layers:

### 1. Preflight Requests (OPTIONS)

Handled in the `preprocessRequest` pipeline (`RewriteUrl` processor) before the
request reaches the handler. When an OPTIONS request arrives at a CORS-enabled
service endpoint with an `Origin` header:

- The request is intercepted and a `204 No Content` response is returned immediately
- No authentication is required (matches browser preflight behavior)

**Preflight response headers:**

| Header | Value |
|---|---|
| `Access-Control-Allow-Origin` | `*` or the request Origin |
| `Access-Control-Allow-Methods` | `GET, POST, OPTIONS` |
| `Access-Control-Allow-Headers` | `Authorization, Content-Type, Content-Encoding` |
| `Access-Control-Max-Age` | Configured value (default `3600`) |
| `Access-Control-Allow-Credentials` | `true` (only if enabled) |

### 2. Actual Requests (GET, POST)

After the request is authenticated and processed by `RemoteScriptCall.ashx`, CORS
headers are appended to the response if the request included an `Origin` header and
the service has CORS configured.

**Actual response headers:**

| Header | Value |
|---|---|
| `Access-Control-Allow-Origin` | `*` or the request Origin |
| `Access-Control-Allow-Credentials` | `true` (only if enabled) |

Methods, headers, and max-age are not included on actual responses -- these are
preflight-only headers per the CORS specification.

## Security Considerations

- **Do not use wildcard origins in production.** A wildcard (`*`) allows any website
  to make cross-origin requests to your SPE API. Use specific origins instead.
- **Credentials and wildcards are mutually exclusive.** The CORS spec forbids
  `Access-Control-Allow-Credentials: true` with `Access-Control-Allow-Origin: *`.
  SPE enforces this at startup -- if both are configured, credentials are disabled
  and a warning is logged.
- **CORS is not authentication.** CORS headers control browser behavior only. They do
  not replace SPE's authentication layer (shared secret, basic auth, bearer tokens).
  All API requests still require valid credentials regardless of CORS configuration.
- **Each service is independent.** Enabling CORS on `restfulv2` does not affect
  `fileDownload` or any other service. Configure only the services that need
  cross-origin access.

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| Browser shows "CORS policy" error | No `<cors>` element on the target service | Add `<cors allowedOrigins="...">` to the service config |
| Preflight returns 401 instead of 204 | SPE version before 9.0, or config not deployed | Verify config is active via `/sitecore/admin/showconfig.aspx` |
| `Access-Control-Allow-Origin` missing on response | Request did not include an `Origin` header, or origin is not in the allowed list | Check the request headers in browser DevTools; verify origin matches config exactly (protocol + host + port) |
| Credentials not sent by browser | `allowCredentials` not set, or using wildcard origins | Set `allowCredentials="true"` with specific (non-wildcard) origins |
| Warning in Sitecore log about CORS misconfiguration | `allowCredentials="true"` combined with `allowedOrigins="*"` | Use specific origins when credentials are needed |
| Headers appear on preflight but not on actual response | Expected behavior | Only `Allow-Origin` and `Allow-Credentials` are sent on actual responses; `Allow-Methods`, `Allow-Headers`, and `Max-Age` are preflight-only |
