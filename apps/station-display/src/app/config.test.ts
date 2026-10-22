import { describe, expect, it } from 'vitest'
import { readStationDisplayConfig } from './config'

const testStationId = '00000000-0000-4000-8000-000000000001'

describe('public station display configuration', () => {
  it('accepts a valid station ID and public API base URL with the default refresh interval', () => {
    expect(
      readStationDisplayConfig({
        VITE_STATION_ID: testStationId,
        VITE_API_BASE_URL: 'https://api.example.test',
      }),
    ).toEqual({
      kind: 'valid',
      config: { stationId: testStationId, apiBaseUrl: 'https://api.example.test', refreshIntervalMs: 30_000 },
    })
  })

  it('accepts a configured positive polling interval', () => {
    expect(readStationDisplayConfig({
      VITE_STATION_ID: testStationId,
      VITE_API_BASE_URL: 'https://api.example.test',
      VITE_STATION_DEPARTURE_REFRESH_INTERVAL_MS: '45000',
    })).toEqual({
      kind: 'valid',
      config: { stationId: testStationId, apiBaseUrl: 'https://api.example.test', refreshIntervalMs: 45_000 },
    })
  })

  it.each(['', '0', '-1', '1.5', 'NaN', 'Infinity', '2147483648', 'not-a-number'])(
    'rejects unsafe polling interval configuration %s',
    (interval) => {
      expect(readStationDisplayConfig({
        VITE_STATION_ID: testStationId,
        VITE_API_BASE_URL: 'https://api.example.test',
        VITE_STATION_DEPARTURE_REFRESH_INTERVAL_MS: interval,
      })).toMatchObject({ kind: 'invalid', reason: 'invalid-refresh-interval' })
    },
  )

  it('rejects missing station ID or API base URL', () => {
    expect(readStationDisplayConfig({ VITE_STATION_ID: '', VITE_API_BASE_URL: 'https://api.example.test' })).toMatchObject({
      kind: 'invalid',
      reason: 'missing-station-id',
    })
    expect(readStationDisplayConfig({ VITE_STATION_ID: testStationId, VITE_API_BASE_URL: '' })).toMatchObject({
      kind: 'invalid',
      reason: 'missing-api-base-url',
    })
  })

  it('rejects malformed or non-UUID station IDs', () => {
    expect(
      readStationDisplayConfig({
        VITE_STATION_ID: 'central-station',
        VITE_API_BASE_URL: 'https://api.example.test',
      }),
    ).toMatchObject({ kind: 'invalid', reason: 'invalid-station-id' })
  })

  it('rejects malformed or non-HTTP API base URLs', () => {
    expect(
      readStationDisplayConfig({ VITE_STATION_ID: testStationId, VITE_API_BASE_URL: '/api' }),
    ).toMatchObject({ kind: 'invalid', reason: 'invalid-api-base-url' })
  })
})
