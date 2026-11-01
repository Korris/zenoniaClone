#!/bin/bash
# One dump, then rotation. Run by backup-loop.sh every day, or by hand:
#   docker compose --env-file .env.prod -f docker-compose.prod.yml exec backup bash /backup-scripts/backup-now.sh
set -eu

BACKUP_DIR="${BACKUP_DIR:-/backups}"
KEEP_DAYS="${BACKUP_KEEP_DAYS:-14}"
NAME="zenonia-$(date -u +%Y%m%d-%H%M%S).archive.gz"
TMP="$BACKUP_DIR/.$NAME.partial"

mkdir -p "$BACKUP_DIR"
trap 'rm -f "$TMP"' EXIT

# --oplog: a point in time across collections, so a trade (two characters and a ledger entry) is never half in the dump. It needs a whole-server dump.
mongodump --uri="$MONGO_URI" --oplog --gzip --archive="$TMP" --quiet
# A dump that cannot be read back is worth nothing, and must not push out good ones.
mongorestore --uri="$MONGO_URI" --dryRun --gzip --archive="$TMP" --quiet
mv "$TMP" "$BACKUP_DIR/$NAME"

# Only reached after a good dump: when dumps start failing, the old ones stay.
find "$BACKUP_DIR" -maxdepth 1 -type f -name 'zenonia-*.archive.gz' -mtime +"$((KEEP_DAYS - 1))" -print -delete
echo "[backup] wrote $NAME ($(du -h "$BACKUP_DIR/$NAME" | cut -f1)), keeping $KEEP_DAYS days"
