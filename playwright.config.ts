import { defineConfig, devices } from '@playwright/test'

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  forbidOnly: Boolean(process.env.CI),
  retries: 0,
  workers: 1,
  reporter: 'list',
  outputDir: './test-results',
  use: {
    baseURL: 'http://127.0.0.1:4173',
    screenshot: 'only-on-failure',
    trace: 'retain-on-failure',
    timezoneId: 'Asia/Colombo',
    colorScheme: 'light',
    reducedMotion: 'reduce',
  },
  projects: [
    {
      name: 'chromium-fixture',
      testIgnore: '**/station-live-contract.spec.ts',
      use: { ...devices['Desktop Chrome'] },
    },
    {
      name: 'chromium-live',
      testMatch: '**/station-live-contract.spec.ts',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: {
    command: 'npm run dev -- --host 127.0.0.1 --port 4173 --strictPort',
    url: 'http://127.0.0.1:4173',
    reuseExistingServer: !process.env.CI,
    timeout: 30_000,
    env: {
      VITE_STATION_ID: '00000000-0000-4000-8000-000000000101',
      VITE_API_BASE_URL: 'http://127.0.0.1:4173/api',
    },
  },
})
