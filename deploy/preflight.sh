#!/bin/bash
# Pre-flight check of the VPS. READ-ONLY: it changes nothing, installs nothing, and never prints a secret.
#   bash deploy/preflight.sh          (with sudo it can also name who holds a port and read ufw and the nginx config)
# Exit code: 0 no FAIL, 1 at least one FAIL. A WARN is for you to read and decide.
# The only thing sent out: one request to a public "what is my IP" service, to compare with DNS.
set -u

HERE="$(cd "$(dirname "$0")" && pwd)"
ENV_FILE="${PREFLIGHT_ENV_FILE:-$HERE/.env.prod}"
fails=0
warns=0
ok() { echo "  [OK]   $1"; }
warn() { echo "  [WARN] $1"; warns=$((warns + 1)); }
fail() { echo "  [FAIL] $1"; fails=$((fails + 1)); }
have() { command -v "$1" >/dev/null 2>&1; }
has_cr() { [ "$(cat "$@" 2>/dev/null | tr -cd '\r' | wc -c)" -gt 0 ]; }
mem_mb() { local kb; kb="$(awk -v k="^$1:" '$0 ~ k {print $2}' /proc/meminfo)"; echo "$(( ${kb:-0} / 1024 ))"; }
# The value of a variable in the env file, for use inside this script only. The file is parsed, never run. Nothing printed may contain a secret.
val() { grep -E "^$1=" "$ENV_FILE" 2>/dev/null | tail -1 | cut -d= -f2- | tr -d '\r' | sed -E 's/^"(.*)"$/\1/; s/^'"'"'(.*)'"'"'$/\1/'; }

echo "== Settings file: $ENV_FILE"
DOMAIN=""; API_PORT=3000; ZONE_PORT=9050
if [ ! -f "$ENV_FILE" ]; then
  fail "not found. Copy deploy/.env.prod from your machine with scp (docs/deploy.md)"
else
  DOMAIN="$(val DOMAIN)"; API_PORT="$(val API_PORT)"; ZONE_PORT="$(val ZONE_PORT)"
  API_PORT="${API_PORT:-3000}"; ZONE_PORT="${ZONE_PORT:-9050}"
  for k in DOMAIN JWT_SECRET SERVICE_KEY; do
    if [ -z "$(val "$k")" ]; then fail "$k is empty: the stack will refuse to start"; else ok "$k is filled"; fi
  done
  for k in SMTP_HOST SMTP_USER SMTP_PASS SMTP_FROM; do
    if [ -z "$(val "$k")" ]; then fail "$k is empty: the API would start but send no password reset mail"; else ok "$k is filled"; fi
  done
  jwt="$(val JWT_SECRET)"; key="$(val SERVICE_KEY)"; pass="$(val SMTP_PASS)"
  if [ -n "$jwt" ] && [ "${#jwt}" -lt 32 ]; then fail "JWT_SECRET is shorter than 32 characters"; fi
  if [ -n "$key" ] && [ "${#key}" -lt 16 ]; then fail "SERVICE_KEY is shorter than 16 characters"; fi
  case "$jwt$key" in *change-me*|*dev-only*) fail "JWT_SECRET or SERVICE_KEY is still a placeholder";; esac
  case "$jwt$key$pass" in *\$*) warn "a secret holds a \$ sign: Docker Compose would try to expand it";; esac
  case "$pass" in *" "*) warn "SMTP_PASS holds spaces: write the App Password without them";; esac
  if [ -n "$pass" ] && [ "${#pass}" -ne 16 ] && [ "$(val SMTP_HOST)" = "smtp.gmail.com" ]; then warn "SMTP_PASS is not 16 characters long: Gmail needs a Google App Password, not the account password"; fi
  if [ "$(val SMTP_HOST)" = "smtp.gmail.com" ] && { [ "$(val SMTP_PORT)" != "587" ] || [ "$(val SMTP_SECURE)" != "false" ]; }; then warn "Gmail is set up here for port 587 with SMTP_SECURE=false (STARTTLS)"; fi
  if has_cr "$ENV_FILE"; then warn "the file has Windows line ends; to fix: sed -i 's/\\r\$//' $ENV_FILE"; fi
  if have stat; then
    mode="$(stat -c %a "$ENV_FILE" 2>/dev/null || true)"
    if [ -n "$mode" ] && [ "$mode" != "600" ] && [ "$mode" != "400" ]; then warn "other users may read it (mode $mode). To fix: chmod 600 $ENV_FILE"; fi
  fi
  unset jwt key pass
fi

echo "== Docker"
if have docker; then
  ok "docker: $(docker --version 2>/dev/null)"
  if docker compose version >/dev/null 2>&1; then ok "compose: $(docker compose version --short 2>/dev/null)"; else fail "the docker compose plugin is missing: see docs/deploy.md, section \"Bước 3. Cài Docker\""; fi
  if docker info >/dev/null 2>&1; then ok "this user can talk to the Docker daemon"; else fail "cannot talk to the Docker daemon: it is stopped, or this user needs: sudo usermod -aG docker \$USER, then log in again"; fi
else
  fail "docker is not installed: see docs/deploy.md, section \"Bước 3. Cài Docker\" (official apt repo; it does not touch nginx)"
fi

echo "== nginx and certbot"
if have nginx; then
  ok "$(nginx -v 2>&1)"
  if have systemctl; then
    if systemctl is-active --quiet nginx; then ok "nginx is running"; else warn "nginx is installed but not running"; fi
  fi
  if [ -d /etc/nginx/sites-available ] && [ -d /etc/nginx/sites-enabled ]; then
    ok "sites-available and sites-enabled exist"
    if [ -r /etc/nginx/nginx.conf ] && ! grep -q 'sites-enabled' /etc/nginx/nginx.conf; then warn "nginx.conf does not include sites-enabled: see the note in docs/deploy.md, step 7"; fi
    echo "         sites enabled now: $(ls /etc/nginx/sites-enabled 2>/dev/null | tr '\n' ' ')"
  else
    warn "no sites-available/sites-enabled: this nginx probably uses conf.d (docs/deploy.md, step 7)"
  fi
  if [ -n "$DOMAIN" ] && [ -d /etc/nginx ]; then
    other="$(grep -rlsE "server_name[^;]*[[:space:]]$DOMAIN([[:space:];]|\$)" /etc/nginx/sites-enabled /etc/nginx/conf.d 2>/dev/null | grep -v 'zenonia.conf' || true)"
    if [ -n "$other" ]; then fail "$DOMAIN is already served by: $other"; else ok "no other site claims $DOMAIN"; fi
  fi
else
  fail "nginx is not installed, though this guide expects it on the host"
fi
if have certbot; then ok "certbot is installed"; else warn "certbot is not installed yet (docs/deploy.md, step 8)"; fi
if have envsubst; then ok "envsubst is installed"; else warn "envsubst is missing: sudo apt install gettext-base"; fi

echo "== Ports"
holder() { # $1 = -t or -u, $2 = port: the listening line(s), if any
  if [ "$1" = "-t" ]; then ss -Hltnp "sport = :$2" 2>/dev/null; else ss -Hlunp "sport = :$2" 2>/dev/null; fi
}
if have ss; then
  for p in 80 443; do
    h="$(holder -t "$p")"
    if [ -z "$h" ]; then warn "nothing listens on TCP $p yet: nginx should"
    else case "$h" in *docker*) warn "TCP $p is held by Docker, not nginx. HTTPS by certbot will not work like this";; *) ok "TCP $p is in use, as expected for nginx";; esac; fi
  done
  h="$(holder -t "$API_PORT")"
  if [ -z "$h" ]; then ok "TCP $API_PORT (API_PORT) is free"
  else warn "TCP $API_PORT (API_PORT) is in use. Fine if this stack is already up. If not, choose another API_PORT. Holder: $(echo "$h" | sed -E 's/ +/ /g')"; fi
  h="$(holder -u "$ZONE_PORT")"
  if [ -z "$h" ]; then ok "UDP $ZONE_PORT (ZONE_PORT) is free"
  else warn "UDP $ZONE_PORT (ZONE_PORT) is in use. Fine if this stack's zone is already up. If not, choose another ZONE_PORT. Holder: $(echo "$h" | sed -E 's/ +/ /g')"; fi
else
  warn "ss is missing, ports were not checked"
fi

echo "== Firewall (ufw)"
if have ufw && [ "$(id -u 2>/dev/null)" = "0" ]; then
  st="$(ufw status 2>/dev/null)"
  case "$st" in
    *inactive*) warn "ufw is inactive. Read step 5 of docs/deploy.md before turning it on";;
    *active*)
      ok "ufw is active"
      echo "$st" | grep -qE "^$ZONE_PORT(/udp)?[[:space:]].*ALLOW" && ok "UDP $ZONE_PORT is allowed" || warn "UDP $ZONE_PORT is not allowed yet: sudo ufw allow $ZONE_PORT/udp"
      echo "$st" | grep -qE "^(22|OpenSSH)(/tcp)?[[:space:]]" || warn "no rule for SSH shows up (if SSH uses another port, check that one)"
      echo "$st" | grep -qE "^(80|443|Nginx)" || warn "no rule for 80/443 shows up: certbot and players need them";;
    *) warn "could not read the ufw status";;
  esac
elif have ufw; then
  warn "run with sudo to read the ufw rules"
else
  warn "ufw is not installed. Check the firewall of your VPS provider too"
fi

echo "== Memory and disk"
if [ -r /proc/meminfo ]; then
  avail_mb="$(mem_mb MemAvailable)"
  if [ "$avail_mb" = 0 ]; then avail_mb="$(mem_mb MemFree)"; fi # very old kernels have no MemAvailable
  total_mb="$(mem_mb MemTotal)"
  swap_mb="$(mem_mb SwapTotal)"
  if [ "$avail_mb" -lt 1000 ]; then fail "only $avail_mb MB of RAM free (of $total_mb MB): Mongo and the API alone want about 1 GB, and the zone comes on top"
  elif [ "$avail_mb" -lt 2000 ]; then warn "$avail_mb MB of RAM free (of $total_mb MB, swap $swap_mb MB): tight with the zone, whose need has not been measured yet"
  else ok "$avail_mb MB of RAM free (of $total_mb MB, swap $swap_mb MB)"; fi
else
  warn "cannot read /proc/meminfo"
fi
if have df; then
  free_gb=$(( $(df -Pk "$HERE" 2>/dev/null | awk 'NR==2 {print $4}') / 1048576 ))
  if [ "$free_gb" -lt 5 ]; then fail "only $free_gb GB of disk free: images, the Unity build and 14 days of backups need more"
  elif [ "$free_gb" -lt 10 ]; then warn "$free_gb GB of disk free"
  else ok "$free_gb GB of disk free"; fi
fi

echo "== DNS"
if [ -z "$DOMAIN" ]; then
  warn "no DOMAIN to check"
elif ! have getent; then
  warn "getent is missing, DNS was not checked"
else
  dns="$(getent ahostsv4 "$DOMAIN" 2>/dev/null | awk '{print $1}' | sort -u | tr '\n' ' ' | sed 's/ $//')"
  dns6="$(getent ahostsv6 "$DOMAIN" 2>/dev/null | awk '{print $1}' | grep -v '^::ffff:' | sort -u | tr '\n' ' ' | sed 's/ $//')"
  mine=""
  if have curl; then mine="$(curl -4 -fsS -m 8 https://api.ipify.org 2>/dev/null || curl -4 -fsS -m 8 https://ifconfig.me 2>/dev/null || true)"; fi
  if [ -z "$dns" ]; then fail "$DOMAIN does not resolve to an IPv4 address yet (step 4; a new record can take some minutes)"
  elif [ -z "$mine" ]; then warn "$DOMAIN points at $dns, but this host's public IP could not be found to compare. Compare by hand"
  elif [ "$dns" = "$mine" ]; then ok "$DOMAIN points at this host ($mine)"
  else fail "$DOMAIN points at $dns but this host is $mine. If a proxy such as Cloudflare is on, set the record to DNS only: game traffic is UDP and does not pass through it"; fi
  if [ -n "$dns6" ]; then warn "$DOMAIN also has an IPv6 record ($dns6): it must be this host too, or remove it"; fi
fi

echo "== Zone build (only needed for the zone, step 11)"
BUILD="$(val ZONE_BUILD_DIR)"; BUILD="${BUILD:-./zone/build}"
case "$BUILD" in /*) ;; *) BUILD="$HERE/$BUILD" ;; esac
EXE="$(val ZONE_EXECUTABLE)"; EXE="${EXE:-Zenonia.x86_64}"
if [ ! -f "$BUILD/$EXE" ]; then warn "no player at $BUILD/$EXE yet. The API works without it; nobody can play until it is there"
elif [ ! -x "$BUILD/$EXE" ]; then warn "$BUILD/$EXE is not executable: chmod +x it"
else ok "player found at $BUILD/$EXE"; fi

echo "== Line ends of the scripts"
if has_cr "$HERE"/backup/*.sh "$HERE"/zone/entrypoint.sh; then fail "a script in deploy/ has Windows line ends and will not run in Linux"; else ok "the scripts have Unix line ends"; fi

echo
echo "Result: $fails FAIL, $warns WARN"
if [ "$fails" -gt 0 ]; then exit 1; fi
exit 0
