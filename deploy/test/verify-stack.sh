#!/bin/bash
# Brings the production stack up without the zone under its own project name, behind a throwaway nginx:alpine running the real site file with
# a self-signed certificate on a high port, checks it from outside, tests a dump and a restore, and removes only what it started.
# Needs a running Docker daemon, curl and openssl. No real secret is used.
#   bash deploy/test/verify-stack.sh
set -u
cd "$(dirname "$0")/.."

PROJECT=zenonia-verify
PORT="${VERIFY_HTTPS_PORT:-18443}"
WORK=test/.work
ENV_FILE="$WORK/verify.env"
BASE="https://localhost:$PORT"
fails=0
ok() { echo "  ok    $1"; }
bad() { echo "  FAIL  $1"; fails=$((fails + 1)); }
expect() { if [ "$2" = "$3" ]; then ok "$1 ($3)"; else bad "$1: wanted $2, got $3"; fi; }
code() { curl -sk -o /dev/null -w '%{http_code}' "$@"; }
post() { curl -sk -o /dev/null -w '%{http_code}' -H 'content-type: application/json' "$@"; }
dc() { docker compose -p "$PROJECT" --env-file "$ENV_FILE" -f docker-compose.prod.yml -f test/docker-compose.verify.yml "$@"; }

docker info >/dev/null 2>&1 || { echo "The Docker daemon is not running."; exit 3; }

rm -rf "$WORK"; mkdir -p "$WORK/backups"
cat > "$ENV_FILE" <<EOF
DOMAIN=localhost
JWT_SECRET=$(openssl rand -hex 48)
SERVICE_KEY=$(openssl rand -hex 32)
API_PORT=${VERIFY_API_PORT:-13000}
PASSWORD_RESET_URL=$BASE/reset
BACKUP_DIR=./test/.work/backups
REGISTER_IP_MAX=5
EOF
SERVICE_KEY_VALUE=$(grep '^SERVICE_KEY=' "$ENV_FILE" | cut -d= -f2)

MSYS_NO_PATHCONV=1 openssl req -x509 -newkey rsa:2048 -nodes -days 1 -subj /CN=localhost -keyout "$WORK/key.pem" -out "$WORK/cert.pem" 2>/dev/null
# The real site file. Only what differs here: the API is a container name, and TLS is what certbot would have added.
sed -e 's/${DOMAIN}/localhost/g' -e 's/127\.0\.0\.1:${API_PORT}/api:3000/g' \
    -e 's|^    listen 80;|    listen 443 ssl;\n    ssl_certificate /etc/nginx/cert.pem;\n    ssl_certificate_key /etc/nginx/key.pem;|' \
    -e '/listen \[::\]:80;/d' nginx/zenonia.conf.template > "$WORK/site.conf"

cleanup() { echo "== tearing down $PROJECT"; dc down -v --remove-orphans >/dev/null 2>&1; rm -rf "$WORK"; }
trap cleanup EXIT

echo "== starting"
dc up -d --build --wait --wait-timeout 180 mongo api nginx backup || { dc logs --tail 40; exit 1; }
dc exec -T nginx nginx -t 2>&1 | tail -1

echo "== routes"
expect "/health" 200 "$(code "$BASE/health")"
expect "/health body" '{"ok":true}' "$(curl -sk "$BASE/health")"
expect "/ is not ours" 404 "$(code "$BASE/")"
expect "/reset" 200 "$(code "$BASE/reset")"
expect "/reset referrer policy" 1 "$(curl -skI "$BASE/reset" | grep -ci 'referrer-policy: no-referrer')"
expect "body over 64k" 413 "$(head -c 70000 /dev/zero | tr '\0' 'x' | post --data-binary @- "$BASE/auth/login")"

echo "== /internal stays closed, also with the right key"
for p in /internal /internal/ /internal/zones /INTERNAL/zones //internal/zones /auth/../internal/zones /auth/%2e%2e/internal/zones /auth/..%2finternal/zones /characters/../internal/zones /%69nternal/zones; do
  expect "$p" 404 "$(code --path-as-is -H "x-service-key: $SERVICE_KEY_VALUE" "$BASE$p")"
done

echo "== register, login, a character"
U="v$(date +%s)"
expect "register" 201 "$(post -d "{\"username\":\"$U\",\"email\":\"$U@example.com\",\"password\":\"password123\"}" "$BASE/auth/register")"
LOGIN=$(curl -sk -H 'content-type: application/json' -H 'X-Forwarded-For: 1.2.3.4' -d "{\"login\":\"$U\",\"password\":\"password123\"}" "$BASE/auth/login")
TOKEN=$(printf '%s' "$LOGIN" | sed -n 's/.*"accessToken":"\([^"]*\)".*/\1/p')
if [ -n "$TOKEN" ]; then ok "login"; else bad "login"; fi
expect "characters with the token" 200 "$(code -H "authorization: Bearer $TOKEN" "$BASE/characters")"
expect "characters without" 401 "$(code "$BASE/characters")"
expect "reset request" 202 "$(post -d "{\"email\":\"$U@example.com\"}" "$BASE/auth/password-reset/request")"

echo "== the client IP reaches the API and cannot be faked"
sleep 1
SEEN_API=$(dc logs --no-log-prefix api | grep '"url":"/auth/login"' | tail -1 | sed -n 's/.*"remoteAddress":"\([^"]*\)".*/\1/p')
SEEN_NGINX=$(dc logs --no-log-prefix nginx | grep '/auth/login' | grep ' 200 ' | tail -1 | cut -d' ' -f1)
if [ -n "$SEEN_API" ] && [ "$SEEN_API" = "$SEEN_NGINX" ]; then ok "API sees $SEEN_API, the address nginx saw"; else bad "API sees '$SEEN_API', nginx saw '$SEEN_NGINX'"; fi
if [ "$SEEN_API" != "1.2.3.4" ]; then ok "a client's own X-Forwarded-For is ignored"; else bad "the API believed a faked X-Forwarded-For"; fi
last=
for i in 1 2 3 4 5 6; do last=$(post -H "X-Forwarded-For: 9.9.9.$i" -d "{\"username\":\"${U}x$i\",\"email\":\"${U}x$i@example.com\",\"password\":\"password123\"}" "$BASE/auth/register"); done
expect "register limit holds with a new fake IP each time" 429 "$last"

echo "== the mail warning, and no token in any log"
if dc logs api | grep -q 'password reset mails will NOT be sent'; then ok "startup warns"; else bad "no startup warning"; fi
if dc logs api nginx | grep -q 'token='; then bad "a token reached a log"; else ok "no token in the logs"; fi

echo "== backup and restore"
dc exec -T backup bash /backup-scripts/backup-now.sh | tail -2
FILE=$(ls -1t "$WORK/backups" | grep archive | head -1)
if [ -n "$FILE" ]; then ok "dump $FILE"; else bad "no dump"; fi
A=$(dc exec -T mongo mongosh --quiet zenonia --eval 'db.users.countDocuments()')
dc exec -T mongo mongosh --quiet zenonia --eval 'db.users.deleteMany({})' >/dev/null
expect "users wiped before the restore" 401 "$(post -d "{\"login\":\"$U\",\"password\":\"password123\"}" "$BASE/auth/login")"
dc stop api >/dev/null
dc exec -T backup bash /backup-scripts/restore.sh "$FILE" --yes 2>&1 | tail -2
dc up -d --wait api nginx >/dev/null
expect "users back" "$A" "$(dc exec -T mongo mongosh --quiet zenonia --eval 'db.users.countDocuments()')"
expect "login after the restore" 200 "$(post -d "{\"login\":\"$U\",\"password\":\"password123\"}" "$BASE/auth/login")"

echo
if [ "$fails" = 0 ]; then echo "ALL OK"; else echo "$fails CHECK(S) FAILED"; fi
exit "$fails"
