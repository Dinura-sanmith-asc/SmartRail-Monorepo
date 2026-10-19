import { defineConfig, devices } from '@playwright/test'

const liveMode = process.env.VITE_STATION_DISPLAY_SOURCE === 'live'
const webServerPort = liveMode ? 5173 : 4173
const webServerHost = liveMode ? 'localhost' : '127.0.0.1'

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  forbidOnly: Boolean(process.env.CI),
  retries: 0,
  workers: 1,
  reporter: 'list',
  outputDir: './test-results',
  use: {
    baseURL: `http://${webServerHost}:${webServerPort}`,
    screenshot: 'only-on-failure',
    trace: 'retain-on-failure',
    timezoneId: 'Asia/Colombo',
    colorScheme: 'light',
    reducedMotion: 'reduce',
  },
  projects: [
    {
      name: 'chromium-fixture',
      testIgnore: '**/station-live-*.spec.ts',
      use: { ...devices['Desktop Chrome'] },
    },
    {
      name: 'chromium-live',
      testMatch: '**/station-live-*.spec.ts',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: {
    command: `npm run dev -- --host ${webServerHost} --port ${webServerPort} --strictPort`,
    url: `http://${webServerHost}:${webServerPort}`,
    reuseExistingServer: !process.env.CI,
    timeout: 30_000,
    env: {
      VITE_STATION_ID: process.env.VITE_STATION_ID ?? '10000000-0000-0000-0000-000000000001',
      VITE_API_BASE_URL: process.env.VITE_API_BASE_URL ?? 'http://127.0.0.1:4173/api',
      VITE_STATION_DISPLAY_SOURCE: process.env.VITE_STATION_DISPLAY_SOURCE ?? 'fixture',
      STATION_DISPLAY_LIVE_CONTROLLER_TOKEN: '',
    },
  },
})
