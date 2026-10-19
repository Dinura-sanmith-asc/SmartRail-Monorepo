import { spawnSync } from 'node:child_process'
import { resolve } from 'node:path'

const stationId = process.env.STATION_DISPLAY_LIVE_STATION_ID
const apiBaseUrl = process.env.STATION_DISPLAY_LIVE_API_BASE_URL
const requiredMutationConfiguration = [
  'STATION_DISPLAY_LIVE_JOURNEY_ID',
  'STATION_DISPLAY_LIVE_CONTROLLER_TOKEN',
  'STATION_DISPLAY_LIVE_ENVIRONMENT',
  'STATION_DISPLAY_LIVE_MUTATION_CONFIRMATION',
]
const missingMutationConfiguration = requiredMutationConfiguration.filter((name) => !process.env[name])

if (!stationId || !apiBaseUrl || missingMutationConfiguration.length > 0) {
  console.error('Live browser acceptance requires STATION_DISPLAY_LIVE_STATION_ID and STATION_DISPLAY_LIVE_API_BASE_URL.')
  if (missingMutationConfiguration.length > 0) {
    console.error(`Authorized update verification also requires: ${missingMutationConfiguration.join(', ')}.`)
  }
  console.error('Set mutation variables only for a dedicated development/test environment; the Controller token is used only by the Node test process.')
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
