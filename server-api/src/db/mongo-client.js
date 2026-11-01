import { MongoClient } from 'mongodb';
import { ensureIndexes } from './indexes.js';

/** Connects, ensures indexes and returns { client, db, close }. */
export async function connectMongo(uri, dbName) {
  const client = new MongoClient(uri, { serverSelectionTimeoutMS: 5000 });
  await client.connect();
  const db = client.db(dbName);
  const hello = await db.admin().command({ hello: 1 });
  if (!hello.setName) {
    await client.close();
    throw new Error('MongoDB is not a replica set: trades need transactions. Start mongod with --replSet (one node is enough), see server-api/README.md');
  }
  await ensureIndexes(db);
  return { client, db, close: () => client.close() };
}
