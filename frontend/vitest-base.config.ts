import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test-setup.ts'],
    fileParallelism: false,
    globals: true,
    isolate: false,
    maxWorkers: 1,
    pool: 'threads',
  },
});
