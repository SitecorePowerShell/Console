#!/usr/bin/env bash
# Demonstrates the raw HTTP/curl API for SPE remoting:
#   1. Mint an HS256 JWT from the shared secret.
#   2. Open a persistent session and run a query.
#   3. Run a second query that re-uses the same session.
#   4. Clean up the session.
#
# Requires: bash, curl, openssl, awk. The CM cert is self-signed in
# the dev container, so `-k` is used throughout.

set -euo pipefail

HOST="${HOST:-https://spe.dev.local}"
USERNAME='sitecore\admin'
SECRET="$(grep '^SPE_SHARED_SECRET=' .env | cut -d= -f2-)"

if [[ -z "$SECRET" ]]; then
  echo "SPE_SHARED_SECRET not found in .env" >&2
  exit 1
fi

# ---------- mint an HS256 JWT ------------------------------------------------
b64url() { openssl base64 -A | tr '+/' '-_' | tr -d '='; }

NOW=$(date +%s)
EXP=$((NOW + 120))
HEADER='{"alg":"HS256","typ":"JWT"}'
# Backslash in the username has to survive JSON encoding (\\) and the shell.
PAYLOAD=$(printf '{"iss":"SPE Remoting","aud":"%s","exp":%d,"name":"%s"}' \
  "$HOST" "$EXP" 'sitecore\\admin')

H=$(printf '%s' "$HEADER"  | b64url)
P=$(printf '%s' "$PAYLOAD" | b64url)
SIG=$(printf '%s.%s' "$H" "$P" | openssl dgst -sha256 -mac HMAC -macopt "key:$SECRET" -binary | b64url)
TOKEN="$H.$P.$SIG"

# ---------- session id (any GUID-shaped string is fine) ---------------------
SID=$(powershell.exe -NoProfile -Command "[guid]::NewGuid().ToString()" | tr -d '\r\n')
echo "Session: $SID"
echo

# ---------- 1st call: opens the session and runs a query --------------------
echo "==> 1st call: create + run"
curl -k -s -X POST \
  "$HOST/-/script/script/?sessionId=$SID&persistentSession=true&rawOutput=true&outputFormat=Raw" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: text/plain" \
  --data 'Get-Item -Path "master:/sitecore/content/Home" | Select-Object -ExpandProperty Name'
echo
echo

# ---------- 2nd call: reuses the session, sees state from the 1st run ------
echo "==> 2nd call: reuse session"
curl -k -s -X POST \
  "$HOST/-/script/script/?sessionId=$SID&persistentSession=true&rawOutput=true&outputFormat=Raw" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: text/plain" \
  --data '$ExecutionContext.SessionState.LanguageMode.ToString() + " | host=" + $Host.Name'
echo
echo

# ---------- 3rd call: cleanup ----------------------------------------------
echo "==> cleanup"
curl -k -s -o /dev/null -w "HTTP %{http_code}\n" -X POST \
  "$HOST/-/script/script/?sessionId=$SID&action=cleanup" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: text/plain" \
  --data ''
