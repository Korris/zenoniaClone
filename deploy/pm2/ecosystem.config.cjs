// pm2 processes of the server package made by Tools/package-server.ps1. Settings come from .env next to this file.
const fs = require('fs');
const path = require('path');

const root = __dirname;
process.loadEnvFile(path.join(root, '.env'));
const e = process.env;
const apiPort = e.API_PORT || '3000';
const zoneEnv = {
  ZONE_DIR: path.join(root, 'zone'),
  DATA_DIR: path.join(root, 'data'),
  ZONE_EXECUTABLE: 'ZenoniaServer.x86_64',
  ZONE_PORT: e.ZONE_PORT || '9050',
  ZONE_ID: e.ZONE_ID || 'frost-hollow',
  ZONE_API_URL: `http://127.0.0.1:${apiPort}`,
  ZONE_EXTRA_ARGS: e.ZONE_EXTRA_ARGS || '',
  ZENONIA_SERVICE_KEY: e.SERVICE_KEY,
};

const apps = [
  {
    name: 'pocketvalor-api',
    cwd: root, // the API reads .env from here
    script: 'api/src/server.js',
    env: {
      NODE_ENV: 'production',
      HOST: '127.0.0.1', // only nginx on this machine reaches it
      PORT: apiPort,
      TRUST_PROXY: 'true',
      ZONE_HOST: e.ZONE_HOST || e.DOMAIN,
      PASSWORD_RESET_URL: `https://${e.DOMAIN}/reset`,
    },
    max_memory_restart: '500M',
  },
];

if (fs.existsSync(path.join(root, 'zone', zoneEnv.ZONE_EXECUTABLE))) {
  apps.push({
    name: 'pocketvalor-zone',
    cwd: root,
    script: 'entrypoint.sh',
    interpreter: 'bash',
    env: zoneEnv,
    treekill: false, // the signal goes to entrypoint.sh alone, which makes the zone save everybody first
    kill_timeout: 60000, // the entrypoint gives up at 45 s
    exp_backoff_restart_delay: 1000,
  });
}

module.exports = { apps };
