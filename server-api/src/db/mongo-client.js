import { MongoClient } from 'mongodb';
import { ensureIndexes } from './indexes.js';

/** Connects, ensures indexes and returns { client, db, close }. */
export async function connectMongo(uri, dbName) {
  const client = new MongoClient(uri, { serverSelectionTimeoutMS: 5000 });
  await client.connect();
  const db = client.db(dbName);
  await ensureIndexes(db);
  return { client, db, close: () => client.close() };
}
