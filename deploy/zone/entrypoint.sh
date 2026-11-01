#!/bin/bash
# Starts the zone and turns the container's stop signal into a graceful stop: --shutdown-file is the stop the zone itself documents
# (ZoneServer.TickShutdown checks it once a second, sends players out, writes their final saves, quits). Whether a Unity player
# treats SIGTERM as Application.Quit is not relied on; it is only the fallback here.
set -u

ZONE_DIR="${ZONE_DIR:-/opt/zenonia}"
DATA_DIR="${DATA_DIR:-/data}"
EXE="$ZONE_DIR/${ZONE_EXECUTABLE:-Zenonia.x86_64}"
STOP_FILE="$DATA_DIR/stop-zone"
GIVE_UP_SECONDS="${ZONE_STOP_SECONDS:-45}" # the zone drains for at most 30 s; compose's stop_grace_period must stay above this

if [ -z "${ZENONIA_SERVICE_KEY:-}" ]; then echo "ZENONIA_SERVICE_KEY is empty" >&2; exit 1; fi
if [ ! -f "$EXE" ]; then echo "No player at $EXE: upload the Linux build to ZONE_BUILD_DIR (docs/deploy.md)" >&2; exit 1; fi
if [ ! -x "$EXE" ]; then echo "$EXE is not executable: run chmod +x on it on the host (an upload from Windows loses the flag)" >&2; exit 1; fi

rm -f "$STOP_FILE" # left from a run that was killed: it would stop this one at once

# The key stays in the environment, where ZonePersistence reads it. It is never an argument.
# shellcheck disable=SC2086  # ZONE_EXTRA_ARGS is meant to split into arguments
"$EXE" -batchmode -nographics -logFile - \
  --mode server \
  --port "${ZONE_PORT:?}" \
  --api-url "${ZONE_API_URL:?}" \
  --zone-id "${ZONE_ID:?}" \
  --save-spool "$DATA_DIR/save-failed" \
  --shutdown-file "$STOP_FILE" \
  ${ZONE_EXTRA_ARGS:-} &
pid=$!

stopping=0
request_stop() {
  stopping=1
  echo "[zone-entrypoint] stop requested, the zone is saving everybody"
  : > "$STOP_FILE"
}
trap request_stop TERM INT

waited=0
while kill -0 "$pid" 2>/dev/null; do
  if [ "$stopping" = 1 ]; then
    if [ "$waited" -ge "$GIVE_UP_SECONDS" ]; then
      echo "[zone-entrypoint] the zone did not stop in ${GIVE_UP_SECONDS} s, sending SIGTERM" >&2
      kill -TERM "$pid" 2>/dev/null
      break
    fi
    waited=$((waited + 1))
  fi
  sleep 1 & wait $! 2>/dev/null
done
wait "$pid"
code=$?
echo "[zone-entrypoint] the zone exited with code $code"
exit "$code"
