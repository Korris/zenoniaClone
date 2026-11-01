#!/bin/bash
# Replaces the database with a dump. Stop the zone and the API first (docs/deploy.md):
#   docker compose ... exec backup bash /backup-scripts/restore.sh zenonia-YYYYMMDD-HHMMSS.archive.gz --yes
set -eu

BACKUP_DIR="${BACKUP_DIR:-/backups}"
FILE="${1:-}"
if [ -z "$FILE" ]; then
  echo "Usage: restore.sh <file in $BACKUP_DIR> --yes" >&2
  ls -1t "$BACKUP_DIR" >&2 || true
  exit 2
fi
case "$FILE" in /*) ;; *) FILE="$BACKUP_DIR/$FILE" ;; esac
if [ ! -f "$FILE" ]; then echo "No such file: $FILE" >&2; exit 2; fi
if [ "${2:-}" != "--yes" ]; then echo "This REPLACES the database with $FILE. Add --yes to go on." >&2; exit 2; fi

mongorestore --uri="$MONGO_URI" --gzip --archive="$FILE" --drop --oplogReplay
echo "[restore] done: $FILE"
