// DEV / TEST ONLY (Tools/wanted-boss-test.ps1): sets the level and, optionally, the position of an OFFLINE character straight in the database,
// so a scripted client can kill a boss. The API has no route for this on purpose. Never wired into the app.
//   node scripts/dev-boost-character.mjs --uri <mongo uri> --db <name> --name <character> --level N [--map <id> --x N --y N] [--wanted-day N] [--large-potions N]
// Refuses anything but a loopback database.
import { MongoClient } from 'mongodb';

const arg = (k) => { const i = process.argv.indexOf(k); return i >= 0 ? process.argv[i + 1] : undefined; };
const uri = arg('--uri');
if (!/^mongodb:\/\/(127\.0\.0\.1|localhost)[:/]/.test(uri ?? '')) throw new Error('dev-boost-character only runs against a loopback database');
const level = Number(arg('--level'));
if (!Number.isInteger(level) || level < 1 || level > 60) throw new Error('--level must be an integer from 1 to 60 (MAX_LEVEL)');
const client = await new MongoClient(uri).connect();
try {
  const col = client.db(arg('--db')).collection('characters');
  const doc = await col.findOne({ nameLower: String(arg('--name')).toLowerCase() });
  if (!doc) throw new Error(`no character ${arg('--name')}`);
  if (doc.session) throw new Error('the character holds a session lock: boost it while it is offline');
  const state = doc.state;
  state.version = 3; state.level = level; state.exp = 0; state.hp = 0; state.mp = 0; // hp / mp 0 = full on load
  if (arg('--x')) { state.x = Number(arg('--x')); state.y = Number(arg('--y')); state.hasPosition = true; state.mapId = arg('--map') ?? state.mapId; }
  if (arg('--wanted-day')) state.wantedDay = Number(arg('--wanted-day'));
  if (arg('--large-potions')) { state.hpPotionLarge = Number(arg('--large-potions')); state.hpPotionPick = 2; } // the quick HP button drinks these first, so a lone hero lives through a raid boss and the Small potions of the reward stay countable
  await col.updateOne({ _id: doc._id }, { $set: { state }, $inc: { rev: 1 } });
  console.log(`boosted ${doc.nameLower}: level ${state.level}, map ${state.mapId}`);
} finally {
  await client.close();
}
