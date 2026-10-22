import { defineConfig, devices } from '@playwright/test'

/** Playwright browser tests use the local Vite development server. */
export default defineConfig({
  testDir: './tests/e2e',
  fullyParallel: true,
  forbidOnly: Boolean(process.env.CI),
  retries: process.env.CI ? 2 : 0,
  reporter: 'list',
  use: {
    baseURL: 'http://127.0.0.1:4173',
    // Traces can contain request headers; keep bearer-token fixtures out of artifacts.
    trace: 'off',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: {
    command: 'npm run dev -- --host 127.0.0.1 --port 4173 --strictPort',
    url: 'http://127.0.0.1:4173',
    reuseExistingServer: !process.env.CI,
    timeout: 30_000,
    env: {
      ...process.env,
      // Local-only fixture values. These are not deployment credentials or endpoints.
      VITE_SMARTRAIL_API_BASE_URL: 'https://api.local.test',
      VITE_COGNITO_ISSUER: 'https://issuer.local.test/pool',
      VITE_COGNITO_MANAGED_LOGIN_DOMAIN: 'https://login.local.test',
      VITE_COGNITO_APP_CLIENT_ID: 'local-public-spa-client',
      VITE_COGNITO_CALLBACK_URI: 'http://127.0.0.1:4173/auth/callback',
      VITE_COGNITO_LOGOUT_URI: 'http://127.0.0.1:4173/',
      VITE_COGNITO_SCOPES: 'openid email',
    },
  },
})
