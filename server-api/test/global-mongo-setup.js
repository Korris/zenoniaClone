import { MongoMemoryReplSet } from 'mongodb-memory-server';

// One in-memory single-node replica set shared by all test files (each file uses its own database; trades need transactions). Set TEST_MONGO_URI to use a real replica set instead.
let mongod;

export async function setup({ provide }) {
  let uri = process.env.TEST_MONGO_URI;
  if (!uri) {
    mongod = await MongoMemoryReplSet.create({ replSet: { count: 1, storageEngine: 'wiredTiger' } });
    uri = mongod.getUri();
  }
  provide('mongoUri', uri);
}

export async function teardown() {
  await mongod?.stop();
}
