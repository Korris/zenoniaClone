#!/bin/bash
# The backup container's main process: one dump a day at BACKUP_TIME_UTC, and one at start if the newest is over a day old.
set -u

HERE="$(cd "$(dirname "$0")" && pwd)"
BACKUP_DIR="${BACKUP_DIR:-/backups}"
AT="${BACKUP_TIME_UTC:-19:00}"
trap 'exit 0' TERM INT

dump() { bash "$HERE/backup-now.sh" || echo "[backup] FAILED, the old backups stay. Check the log above" >&2; }

mkdir -p "$BACKUP_DIR"
if [ -z "$(find "$BACKUP_DIR" -maxdepth 1 -type f -name 'zenonia-*.archive.gz' -mtime -1 -print -quit)" ]; then dump; fi

while true; do
  now=$(date -u +%s)
  next=$(date -u -d "$AT" +%s)
  if [ "$next" -le "$now" ]; then next=$((next + 86400)); fi
  echo "[backup] next dump at $(date -u -d "@$next" '+%Y-%m-%d %H:%M') UTC"
  sleep "$((next - now))" & wait $!
  dump
done
