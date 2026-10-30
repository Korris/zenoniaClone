import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    globalSetup: ['./test/global-mongo-setup.js'],
    testTimeout: 30000,
    hookTimeout: 120000,
    fileParallelism: true,
  },
});
