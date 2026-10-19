import { HttpClientError, type HttpClient } from '../../../shared/api/http-client'
import { findPublishedStation } from './station-reference-client'
import type { StationDepartureSource } from './station-departure-source'
import type { OperationalInformation, PublishedValue, StationDepartureSnapshot } from '../model/display-model'

const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i
const statuses = ['Scheduled', 'Running', 'Delayed', 'Interrupted', 'Cancelled', 'Completed', 'Terminated']
const sources = ['ScheduledBaseline', 'ControllerManual', 'ApprovedRailwayFeed']

interface StationDepartureResponse {
  journeyInstanceId: string
  serviceId: string
  operationDate: string
  stationLocalDepartureDate: string
  revision: number
  status: string
  stationId: string
  stationName?: string
  stopOrder: number
  scheduledDepartureAt?: string | null
  operationalDepartureAt?: string | null
  platform?: string | null
  source?: string
  updatedAt?: string
  ambiguity: 'none' | 'ambiguous'
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

function isDateTime(value: unknown): value is string {
  return typeof value === 'string' && /^\d{4}-\d{2}-\d{2}T.+(?:Z|[+-]\d{2}:\d{2})$/i.test(value) && Number.isFinite(Date.parse(value))
}

function isDeparture(value: unknown): value is StationDepartureResponse {
  if (!isRecord(value)) return false
  return (
    typeof value.journeyInstanceId === 'string' && uuid.test(value.journeyInstanceId) &&
    typeof value.serviceId === 'string' && uuid.test(value.serviceId) &&
    typeof value.operationDate === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(value.operationDate) &&
    typeof value.stationLocalDepartureDate === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(value.stationLocalDepartureDate) &&
    Number.isInteger(value.revision) && (value.revision as number) >= 1 &&
    typeof value.status === 'string' && statuses.includes(value.status) &&
    typeof value.stationId === 'string' && uuid.test(value.stationId) &&
    Number.isInteger(value.stopOrder) && (value.stopOrder as number) >= 1 &&
    (value.scheduledDepartureAt === undefined || value.scheduledDepartureAt === null || isDateTime(value.scheduledDepartureAt)) &&
    (value.operationalDepartureAt === undefined || value.operationalDepartureAt === null || isDateTime(value.operationalDepartureAt)) &&
    (value.platform === undefined || value.platform === null || typeof value.platform === 'string') &&
    (value.stationName === undefined || typeof value.stationName === 'string') &&
    (value.source === undefined || (typeof value.source === 'string' && sources.includes(value.source))) &&
    (value.updatedAt === undefined || isDateTime(value.updatedAt)) &&
    (value.ambiguity === 'none' || value.ambiguity === 'ambiguous')
  )
}

function localDate(now: Date): string {
  const parts = new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Asia/Colombo', year: 'numeric', month: '2-digit', day: '2-digit',
  }).formatToParts(now)
  const part = (type: string) => parts.find((item) => item.type === type)?.value
  return `${part('year')}-${part('month')}-${part('day')}`
}

function localTime(value: string | null | undefined): PublishedValue<string> {
  if (!value) return { kind: 'unknown' }
  return {
    kind: 'known',
    value: new Intl.DateTimeFormat('en-GB', {
      timeZone: 'Asia/Colombo', hour: '2-digit', minute: '2-digit', hourCycle: 'h23',
    }).format(new Date(value)),
  }
}

function stringValue(value: string | null | undefined): PublishedValue<string> {
  return typeof value === 'string' ? { kind: 'known', value } : { kind: 'unknown' }
}

function mapOperational(item: StationDepartureResponse): OperationalInformation {
  return {
    status: { kind: 'known', value: item.status },
    platform: stringValue(item.platform),
    operationalDepartureTime: localTime(item.operationalDepartureAt),
    delay: { kind: 'unknown' },
    cancellation: { kind: 'known', value: item.status === 'Cancelled' },
    ...(item.updatedAt ? { updateTime: { displayValue: item.updatedAt, meaning: 'unverified' as const } } : {}),
  }
}

export function createStationDepartureAdapter(client: Pick<HttpClient, 'get'>, now: () => Date = () => new Date()): StationDepartureSource {
  const read = async (stationId: string): Promise<StationDepartureSnapshot> => {
    const stationResult = await findPublishedStation(stationId, client)
    if (stationResult.kind !== 'found') {
      throw new HttpClientError('http', stationResult.kind === 'unpublished' ? 'The configured station is not published.' : 'Station information is unavailable.')
    }

    const date = localDate(now())
    const path = `/stations/${encodeURIComponent(stationId)}/departures?date=${date}`
    const payload = await client.get<unknown>(path)
    if (!Array.isArray(payload) || !payload.every(isDeparture) || payload.some((item) =>
      item.stationId !== stationId || item.stationLocalDepartureDate !== date)) {
      throw new HttpClientError('malformed-response', 'The server returned a departure response outside the approved contract.')
    }

    return {
      station: stationResult.station,
      origin: 'authoritative',
      departures: payload.map((item) => {
        const scheduled = localTime(item.scheduledDepartureAt)
        return {
          departureId: `${item.journeyInstanceId}:${item.stopOrder}`,
          serviceDate: item.stationLocalDepartureDate,
          serviceLabel: `Service ID ${item.serviceId}`,
          scheduledTime: { displayValue: scheduled.kind === 'known' ? scheduled.value : 'Unknown' },
          operational: mapOperational(item),
          sourceRevision: item.revision,
          identityAmbiguous: item.ambiguity === 'ambiguous',
        }
      }),
    }
  }

  return {
    getInitialSnapshot: read,
    revalidate: read,
    // Journeys & Status v1.0.0 defines snapshots and history reads, not a push subscription.
    subscribe: () => () => undefined,
  }
}
