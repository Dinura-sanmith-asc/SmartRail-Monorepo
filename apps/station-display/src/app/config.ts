export interface StationDisplayConfig {
  stationId: string
  apiBaseUrl: string
  refreshIntervalMs: number
}

export type ConfigErrorReason =
  | 'missing-station-id'
  | 'invalid-station-id'
  | 'missing-api-base-url'
  | 'invalid-api-base-url'
  | 'invalid-refresh-interval'

export type StationDisplayConfigResult =
  | { kind: 'valid'; config: StationDisplayConfig }
  | { kind: 'invalid'; reason: ConfigErrorReason }

export interface PublicStationDisplayEnvironment {
  VITE_STATION_ID?: string
  VITE_API_BASE_URL?: string
  VITE_STATION_DEPARTURE_REFRESH_INTERVAL_MS?: string
}

export const DEFAULT_STATION_DEPARTURE_REFRESH_INTERVAL_MS = 30_000
const MAX_TIMER_INTERVAL_MS = 2_147_483_647
const UUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

/**
 * These VITE_* values are public and normally embedded into the frontend at build time.
 * Configure VITE_STATION_ID, VITE_API_BASE_URL, and optionally
 * VITE_STATION_DEPARTURE_REFRESH_INTERVAL_MS in the deployment build environment.
 * VITE_* values are public and normally embedded at build time. The API base URL
 * includes the API path prefix (for example, https://api.example.test/api).
 * Never put credentials or secrets in frontend environment variables.
 */
export function readStationDisplayConfig(
  env: PublicStationDisplayEnvironment = {
    VITE_STATION_ID: import.meta.env.VITE_STATION_ID,
    VITE_API_BASE_URL: import.meta.env.VITE_API_BASE_URL,
    VITE_STATION_DEPARTURE_REFRESH_INTERVAL_MS: import.meta.env.VITE_STATION_DEPARTURE_REFRESH_INTERVAL_MS,
  },
): StationDisplayConfigResult {
  const stationId = env.VITE_STATION_ID?.trim()
  const apiBaseUrl = env.VITE_API_BASE_URL?.trim()

  if (!stationId) return { kind: 'invalid', reason: 'missing-station-id' }
  if (!UUID_PATTERN.test(stationId)) return { kind: 'invalid', reason: 'invalid-station-id' }
  if (!apiBaseUrl) return { kind: 'invalid', reason: 'missing-api-base-url' }

  try {
    const url = new URL(apiBaseUrl)
    if (!['http:', 'https:'].includes(url.protocol) || url.username || url.password || url.search || url.hash) {
      return { kind: 'invalid', reason: 'invalid-api-base-url' }
    }
  } catch {
    return { kind: 'invalid', reason: 'invalid-api-base-url' }
  }

  const configuredInterval = env.VITE_STATION_DEPARTURE_REFRESH_INTERVAL_MS
  let refreshIntervalMs = DEFAULT_STATION_DEPARTURE_REFRESH_INTERVAL_MS
  if (configuredInterval !== undefined) {
    const normalizedInterval = configuredInterval.trim()
    const parsedInterval = Number(normalizedInterval)
    if (
      !/^\d+$/.test(normalizedInterval) ||
      !Number.isSafeInteger(parsedInterval) ||
      parsedInterval < 1 ||
      parsedInterval > MAX_TIMER_INTERVAL_MS
    ) {
      return { kind: 'invalid', reason: 'invalid-refresh-interval' }
    }
    refreshIntervalMs = parsedInterval
  }

  return { kind: 'valid', config: { stationId, apiBaseUrl, refreshIntervalMs } }
}
