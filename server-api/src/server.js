import { buildApp } from './app.js';
import { loadConfig } from './config.js';
import { connectMongo } from './db/mongo-client.js';

// Optional .env in the working directory (Node 20.12+). Real environment variables win.
try { process.loadEnvFile(); } catch { /* no .env file: use the process environment */ }

const cfg = loadConfig();
const mongo = await connectMongo(cfg.MONGO_URI, cfg.MONGO_DB);
const app = buildApp({ db: mongo.db, cfg, logger: { level: process.env.LOG_LEVEL ?? 'info', redact: ['req.headers.authorization', 'req.headers["x-service-key"]'] } });

async function shutdown(signal) {
  app.log.info({ signal }, 'shutting down');
  try {
    await app.close();
    await mongo.close();
    process.exit(0);
  } catch (err) {
    app.log.error({ err }, 'error during shutdown');
    process.exit(1);
  }
}
process.on('SIGINT', () => shutdown('SIGINT'));
process.on('SIGTERM', () => shutdown('SIGTERM'));

await app.listen({ port: cfg.PORT, host: cfg.HOST });
