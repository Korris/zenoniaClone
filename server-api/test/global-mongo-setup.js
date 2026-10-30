import { MongoMemoryServer } from 'mongodb-memory-server';

// One in-memory mongod shared by all test files (each file uses its own database). Set TEST_MONGO_URI to use a real Mongo instead.
let mongod;

export async function setup({ provide }) {
  let uri = process.env.TEST_MONGO_URI;
  if (!uri) {
    mongod = await MongoMemoryServer.create();
    uri = mongod.getUri();
  }
  provide('mongoUri', uri);
}

export async function teardown() {
  await mongod?.stop();
}
