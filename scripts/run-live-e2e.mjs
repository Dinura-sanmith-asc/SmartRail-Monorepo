import { spawnSync } from 'node:child_process'
import { resolve } from 'node:path'

const stationId = process.env.STATION_DISPLAY_LIVE_STATION_ID
const apiBaseUrl = process.env.STATION_DISPLAY_LIVE_API_BASE_URL

if (!stationId || !apiBaseUrl) {
  console.error('Set STATION_DISPLAY_LIVE_STATION_ID and STATION_DISPLAY_LIVE_API_BASE_URL to run live acceptance.')
  process.exit(2)
}

const result = spawnSync(
  process.execPath,
  [resolve('node_modules/@playwright/test/cli.js'), 'test', '--project=chromium-live'],
  {
    stdio: 'inherit',
    env: {
      ...process.env,
      VITE_STATION_ID: stationId,
      VITE_API_BASE_URL: apiBaseUrl,
      VITE_STATION_DISPLAY_SOURCE: 'live',
    },
  },
)

if (result.error) throw result.error
process.exit(result.status ?? 1)
