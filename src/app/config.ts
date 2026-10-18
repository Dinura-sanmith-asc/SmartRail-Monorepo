export interface StationDisplayConfig {
  stationId: string
  apiBaseUrl: string
}

export type ConfigErrorReason =
  | 'missing-station-id'
  | 'invalid-station-id'
  | 'missing-api-base-url'
  | 'invalid-api-base-url'

export type StationDisplayConfigResult =
  | { kind: 'valid'; config: StationDisplayConfig }
  | { kind: 'invalid'; reason: ConfigErrorReason }

export interface PublicStationDisplayEnvironment {
  VITE_STATION_ID?: string
  VITE_API_BASE_URL?: string
}

const UUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

/**
 * These VITE_* values are public and normally embedded into the frontend at build time.
 * Configure VITE_STATION_ID and VITE_API_BASE_URL in the deployment build environment;
 * the API base URL includes the API path prefix (for example, https://api.example.test/api).
 * Never put credentials or secrets in frontend environment variables.
 */
export function readStationDisplayConfig(
  env: PublicStationDisplayEnvironment = {
    VITE_STATION_ID: import.meta.env.VITE_STATION_ID,
    VITE_API_BASE_URL: import.meta.env.VITE_API_BASE_URL,
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

  return { kind: 'valid', config: { stationId, apiBaseUrl } }
}
