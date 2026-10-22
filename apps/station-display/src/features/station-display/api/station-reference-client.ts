import { HttpClientError, type HttpClient } from '../../../shared/api/http-client'
import type { StationContext } from '../model/display-model'

interface StationList {
  items: StationContext[]
}

export type StationLookupResult =
  | { kind: 'found'; station: StationContext }
  | { kind: 'unpublished' }
  | { kind: 'unavailable'; failure: 'network' | 'http' | 'malformed-response' }

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

function isUuid(value: unknown): value is string {
  return (
    typeof value === 'string' &&
    /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value)
  )
}

function isStationList(value: unknown): value is StationList {
  return (
    isRecord(value) &&
    Array.isArray(value.items) &&
    value.items.every(
      (station) =>
        isRecord(station) &&
        isUuid(station.stationId) &&
        typeof station.name === 'string' &&
        station.name.trim().length > 0 &&
        (station.locationDetails === undefined ||
          (Array.isArray(station.locationDetails) && station.locationDetails.every((item) => typeof item === 'string'))) &&
        (station.serviceDetails === undefined ||
          (Array.isArray(station.serviceDetails) && station.serviceDetails.every((item) => typeof item === 'string'))),
    )
  )
}

export async function findPublishedStation(
  configuredStationId: string,
  client: Pick<HttpClient, 'get'>,
): Promise<StationLookupResult> {
  let response: unknown
  try {
    response = await client.get<unknown>('/stations')
  } catch (error) {
    const failure = error instanceof HttpClientError ? error.kind : 'network'
    return { kind: 'unavailable', failure }
  }

  if (!isStationList(response)) return { kind: 'unavailable', failure: 'malformed-response' }

  const station = response.items.find((item) => item.stationId === configuredStationId)
  return station ? { kind: 'found', station: { stationId: station.stationId, name: station.name } } : { kind: 'unpublished' }
}
