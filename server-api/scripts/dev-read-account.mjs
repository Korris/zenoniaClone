// DEV / TEST ONLY: prints the stored account (pets, tickets, pity, history) of a character's user as one JSON line (Tools/net-persist-test.ps1 pet section).
//   node scripts/dev-read-account.mjs --uri <mongo uri> --db <name> --name <character>
// Read only. Refuses anything but a loopback database. Prints {"rev":-1} when the account does not exist yet.
import { MongoClient } from 'mongodb';

const arg = (k) => { const i = process.argv.indexOf(k); return i >= 0 ? process.argv[i + 1] : undefined; };
const uri = arg('--uri');
if (!/^mongodb:\/\/(127\.0\.0\.1|localhost)[:/]/.test(uri ?? '')) throw new Error('dev-read-account only runs against a loopback database');
const client = await new MongoClient(uri).connect();
try {
  const db = client.db(arg('--db'));
  const doc = await db.collection('characters').findOne({ nameLower: arg('--name').toLowerCase() });
  if (!doc) throw new Error(`no character ${arg('--name')}`);
  const account = await db.collection('accounts').findOne({ _id: doc.userId });
  console.log(JSON.stringify(account ? { rev: account.rev, state: account.state } : { rev: -1 }));
} finally {
  await client.close();
}
