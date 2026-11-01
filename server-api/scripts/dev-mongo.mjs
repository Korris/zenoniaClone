// DEV ONLY: a real mongod (the binary mongodb-memory-server downloads) as a one-node replica set on loopback, for when Docker is not running.
//   node scripts/dev-mongo.mjs [--port 27117] [--db-path .dev-data]
// Runs until killed. Prints "READY <uri>". With --db-path the data survives restarts.
import fs from 'node:fs';
import path from 'node:path';
import { MongoMemoryReplSet } from 'mongodb-memory-server';

const arg = (k, d) => { const i = process.argv.indexOf(k); return i >= 0 ? process.argv[i + 1] : d; };
const port = Number(arg('--port', '27117'));
const dbPath = arg('--db-path', '.dev-data');
const full = path.resolve(dbPath);
fs.mkdirSync(full, { recursive: true });
// A replica set remembers its port: the same folder always comes back on the same one.
const rs = await MongoMemoryReplSet.create({
  replSet: { count: 1, name: 'rs0', storageEngine: 'wiredTiger', ip: '127.0.0.1' },
  instanceOpts: [{ port, dbPath: full, storageEngine: 'wiredTiger' }],
});
console.log(`READY mongodb://127.0.0.1:${port}/?replicaSet=rs0`);
void rs;
const stop = async () => { await rs.stop({ doCleanup: false, force: false }); process.exit(0); };
process.on('SIGINT', stop);
process.on('SIGTERM', stop);
setInterval(() => {}, 1 << 30);
