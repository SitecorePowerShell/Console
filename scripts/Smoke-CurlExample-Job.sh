#!/usr/bin/env bash
# Demonstrates the long-poll + stream-tee pattern over raw HTTP/curl:
#   1. Mint an HS256 JWT.
#   2. POST a script that wraps the work in Start-ScriptSession; this spawns
#      a background runspace and returns its session id (the "job id").
#   3. Long-poll GET /-/script/wait/?cursor=... until isDone=true. Each
#      response carries any Verbose/Information/Progress/Warning records
#      the script emitted since the last cursor + a fresh cursor for next time.
#   4. POST a follow-up script that does Receive-ScriptSession to drain the
#      runspace's Output (the script's return value).
#   5. Cleanup the outer session.

set -euo pipefail

HOST="${HOST:-https://spe.dev.local}"
SECRET="$(grep '^SPE_SHARED_SECRET=' .env | cut -d= -f2-)"

# ---------- mint an HS256 JWT ----------
b64url() { openssl base64 -A | tr '+/' '-_' | tr -d '='; }

NOW=$(date +%s); EXP=$((NOW + 600))
HEADER='{"alg":"HS256","typ":"JWT"}'
PAYLOAD=$(printf '{"iss":"SPE Remoting","aud":"%s","exp":%d,"name":"%s"}' \
  "$HOST" "$EXP" 'sitecore\\admin')

H=$(printf '%s' "$HEADER"  | b64url)
P=$(printf '%s' "$PAYLOAD" | b64url)
SIG=$(printf '%s.%s' "$H" "$P" | \
      openssl dgst -sha256 -mac HMAC -macopt "key:$SECRET" -binary | b64url)
TOKEN="$H.$P.$SIG"
AUTH="Authorization: Bearer $TOKEN"

# Outer session id (the caller's session).
SID=$(powershell.exe -NoProfile -Command "[guid]::NewGuid().ToString()" | tr -d '\r\n')
echo "Outer session: $SID"

# ---------- 1. Launch the job ----------
echo
echo "==> launch (Start-ScriptSession returns the background session id)"
LAUNCH_SCRIPT='Start-ScriptSession -ScriptBlock { 1..3 | ForEach-Object { Write-Verbose ("step " + $_) -Verbose; Write-Progress -Activity "demo" -Status ("step " + $_) -PercentComplete ($_ * 33); Start-Sleep -Seconds 5 }; Write-Warning "done"; "DONE" } | Select-Object -ExpandProperty ID'
JOB_ID=$(curl -k -fsS -X POST \
  "$HOST/-/script/script/?sessionId=$SID&persistentSession=true&rawOutput=true&outputFormat=Raw" \
  -H "$AUTH" -H "Content-Type: text/plain" \
  --data "$LAUNCH_SCRIPT" | tr -d '\r\n')
echo "Job (background session) id: $JOB_ID"

# ---------- 2. Long-poll with cursor; print streams as they arrive ----------
# JSON parsing via powershell.exe (jq/python aren't always available on dev boxes).
echo
echo "==> long-poll wait with stream cursor (timeoutSeconds=60 per call)"
START=$(date +%s)
CURSOR=""

# Helper that takes a JSON body on stdin and prints one summary line plus
# zero-or-more "  [stream] ..." record lines, then on the LAST line emits the
# cursor (or empty) prefixed with "CURSOR=" so the bash side can capture it.
parse_response() {
    powershell.exe -NoProfile -Command '
$body = [Console]::In.ReadToEnd()
$d = $body | ConvertFrom-Json
$count = if ($d.streams) { @($d.streams).Count } else { 0 }
$dropped = if ($d.PSObject.Properties.Name -contains "droppedCount") { $d.droppedCount } else { 0 }
Write-Output "isDone=$($d.isDone) status=$($d.status) streams=$count dropped=$dropped elapsed=$($d.elapsedSeconds)s"
foreach ($s in @($d.streams)) {
    if ($s.stream -eq "progress") {
        Write-Output ("  [{0}] {1} - {2}% ({3})" -f $s.stream, $s.activity, $s.percentComplete, $s.statusDescription)
    } else {
        Write-Output ("  [{0}] {1}" -f $s.stream, $s.message)
    }
}
if ($d.cursor) { Write-Output "CURSOR=$($d.cursor)" } else { Write-Output "CURSOR=" }
'
}

while :; do
    URL="$HOST/-/script/wait/?sessionId=$SID&jobId=$(printf '%s' "$JOB_ID")&jobType=scriptsession&timeoutSeconds=60"
    if [[ -n "$CURSOR" ]]; then
        URL+="&cursor=$(printf '%s' "$CURSOR" | sed 's/+/%2B/g; s/=/%3D/g; s/\//%2F/g')"
    else
        URL+="&cursor="
    fi

    RESP=$(curl -k -fsS -G "$URL" -H "$AUTH")
    NOW=$(date +%s); ELAPSED=$((NOW - START))

    PARSED=$(printf '%s' "$RESP" | parse_response)
    SUMMARY=$(printf '%s\n' "$PARSED" | head -n 1 | tr -d '\r')
    RECORDS=$(printf '%s\n' "$PARSED" | sed -n '2,$p' | grep -v '^CURSOR=' | tr -d '\r')
    CURSOR=$(printf '%s\n' "$PARSED" | grep '^CURSOR=' | head -n 1 | sed 's/^CURSOR=//' | tr -d '\r')

    echo "[t+${ELAPSED}s] $SUMMARY"
    if [[ -n "$RECORDS" ]]; then
        printf '%s\n' "$RECORDS"
    fi

    if echo "$RESP" | grep -q '"isDone":true'; then break; fi
done

# ---------- 3. Drain the buffered Output ----------
echo
echo "==> drain Output via Receive-ScriptSession"
DRAIN_SCRIPT="Get-ScriptSession -Id '$JOB_ID' | Receive-ScriptSession"
curl -k -fsS -X POST \
  "$HOST/-/script/script/?sessionId=$SID&persistentSession=true&rawOutput=true&outputFormat=Raw" \
  -H "$AUTH" -H "Content-Type: text/plain" \
  --data "$DRAIN_SCRIPT"
echo

# ---------- 4. Cleanup ----------
echo
echo "==> cleanup outer session"
curl -k -fsS -o /dev/null -w "HTTP %{http_code}\n" -X POST \
  "$HOST/-/script/script/?sessionId=$SID&action=cleanup" \
  -H "$AUTH" -H "Content-Type: text/plain" --data ''
