import { defineConfig } from '@playwright/test';

/**
 * MIG-001 F7 browser suite. Runs only through `stack/run-f7.mjs`, which
 * provisions disposable databases and starts NestJS + Vite; the config itself
 * never starts servers. Uses the locally installed Chrome (no browser download).
 */
export default defineConfig({
  testDir: './specs',
  outputDir: './test-results',
  timeout: 60_000,
  expect: { timeout: 10_000 },
  fullyParallel: false,
  forbidOnly: true,
  retries: 0,
  workers: 2,
  reporter: [['list'], ['json', { outputFile: '.state/results.json' }]],
  use: {
    baseURL: process.env['E2E_BASE_URL'] ?? 'http://127.0.0.1:5173',
    channel: 'chrome',
    headless: true,
    viewport: { width: 1440, height: 900 },
    locale: 'es-BO',
    timezoneId: 'America/La_Paz',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'off',
    actionTimeout: 10_000,
    navigationTimeout: 20_000,
  },
});
