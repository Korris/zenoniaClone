// DEV / TEST ONLY: gives an OFFLINE character test goods straight in the database (Tools/net-persist-test.ps1). The API has no route for this on purpose.
//   node scripts/dev-seed.mjs --uri <mongo uri> --db <name> --name <character> [--gold N] [--stones N] [--item <templateId>:<uid>] [--pet <templateId>:<uid>] [--card <cardId>:<n>[,<cardId>:<n>...]] [--quest <questId>:<state 1-3>[:<progress>][,...]] [--pet-durability <uid>:<n>] [--map <id> --x N --y N]
// Refuses anything but a loopback database.
import { MongoClient } from 'mongodb';

const arg = (k) => { const i = process.argv.indexOf(k); return i >= 0 ? process.argv[i + 1] : undefined; };
const uri = arg('--uri');
if (!/^mongodb:\/\/(127\.0\.0\.1|localhost)[:/]/.test(uri ?? '')) throw new Error('dev-seed only runs against a loopback database');
const client = await new MongoClient(uri).connect();
const col = client.db(arg('--db')).collection('characters');
const name = arg('--name').toLowerCase();
const doc = await col.findOne({ nameLower: name });
if (!doc) throw new Error(`no character ${name}`);
if (doc.session) console.log('warning: the character holds a session lock');
const state = doc.state;
if (arg('--gold')) state.gold = Number(arg('--gold'));
if (arg('--stones')) state.upgradeStone = Number(arg('--stones'));
if (arg('--level')) state.level = Number(arg('--level')); // the pet combat slot opens at Lv 5 (PetRules.SlotUnlockLevel)
if (arg('--item')) {
  const [templateId, uid] = arg('--item').split(':');
  state.inventory.push({ uid, templateId, slot: 'armor', name: 'Leather Vest', rarity: 'common', classId: '', itemLevel: 1, enhance: 0, stats: { atk: 0, def: 2, maxHp: 10, maxMp: 0, critChance: 0, speedMult: 0 } });
}
if (arg('--pet')) {
  const [templateId, uid] = arg('--pet').split(':');
  state.inventory.push({ uid, templateId, slot: 'pet', name: 'Pet', rarity: 'rare', classId: '', itemLevel: 1, enhance: 0, stats: { atk: 0, def: 0, maxHp: 0, maxMp: 0, critChance: 0, speedMult: 0 } });
}
if (arg('--card') || arg('--quest')) state.version = Math.max(state.version ?? 0, 7); // a save older than v7 is turned into a NEW character by the zone (Taurus V4): seeded cards / quests only survive in a v7 state
for (const entry of (arg('--card') ?? '').split(',').filter(Boolean)) {
  const [id, n] = entry.split(':');
  state.cards = [...(state.cards ?? []).filter((c) => c.id !== id), { id, n: Number(n ?? 1), locked: 0 }];
}
for (const entry of (arg('--quest') ?? '').split(',').filter(Boolean)) { // a quest in a given state (1 Active, 2 ReadyToTurnIn, 3 Done): the Taurus chain before Orin
  const [id, st, progress] = entry.split(':');
  state.quests = [...(state.quests ?? []).filter((q) => q.id !== id), { id, state: Number(st), progress: Number(progress ?? 5) }];
}
if (arg('--pet-durability')) {
  const [uid, n] = arg('--pet-durability').split(':');
  const pet = [...state.inventory, ...state.equipment].find((i) => i.uid === uid);
  if (!pet) throw new Error(`no pet ${uid}`);
  pet.durability = Number(n);
}
if (arg('--x')) { state.x = Number(arg('--x')); state.y = Number(arg('--y')); state.hasPosition = true; state.mapId = arg('--map') ?? state.mapId; }
await col.updateOne({ _id: doc._id }, { $set: { state }, $inc: { rev: 1 } });
await client.close();
console.log(`seeded ${name}: gold ${state.gold}, stones ${state.upgradeStone}, bag ${state.inventory.length}`);
