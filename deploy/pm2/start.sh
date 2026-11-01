#!/bin/bash
# Run on the server after unpacking: installs what the API needs and starts or restarts everything under pm2.
set -eu
cd "$(dirname "$0")"

command -v node >/dev/null || { echo "Node is not installed (20.12 or newer)"; exit 1; }
command -v pm2 >/dev/null || { echo "pm2 is not installed: npm install -g pm2"; exit 1; }
node -e 'const [a,b]=process.versions.node.split(".").map(Number); process.exit(a>20||(a===20&&b>=12)?0:1)' || { echo "Node $(node -v) is too old: need 20.12 or newer"; exit 1; }

chmod 600 .env
mkdir -p data
[ -f zone/ZenoniaServer.x86_64 ] && chmod +x zone/ZenoniaServer.x86_64 # an archive made on Windows loses the flag
(cd api && npm ci --omit=dev --no-audit --no-fund)
pm2 startOrRestart ecosystem.config.cjs --update-env
pm2 save
pm2 list
echo "Logs: pm2 logs"
