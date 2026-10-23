import { HttpClientError, type HttpClient } from '../../../shared/api/http-client'
import { findPublishedStation } from './station-reference-client'
import type { StationDepartureSource } from './station-departure-source'
import type { OperationalInformation, PublishedValue, StationDepartureSnapshot } from '../model/display-model'
import { DEFAULT_STATION_DEPARTURE_REFRESH_INTERVAL_MS } from '../../../app/config'

const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i
const statuses = ['Scheduled', 'Running', 'Delayed', 'Interrupted', 'Cancelled', 'Completed', 'Terminated']
const sources = ['ScheduledBaseline', 'ControllerManual', 'StationStaffManual', 'ApprovedRailwayFeed']

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
  scheduledArrivalAt?: string | null
  operationalArrivalAt?: string | null
  serviceCode?: string | null
  displayName?: string | null
  publicServiceNumber?: string | null
  originName?: string | null
  destinationName?: string | null
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
    (value.scheduledArrivalAt === undefined || value.scheduledArrivalAt === null || isDateTime(value.scheduledArrivalAt)) &&
    (value.operationalArrivalAt === undefined || value.operationalArrivalAt === null || isDateTime(value.operationalArrivalAt)) &&
    (value.serviceCode === undefined || value.serviceCode === null || typeof value.serviceCode === 'string') &&
    (value.displayName === undefined || value.displayName === null || typeof value.displayName === 'string') &&
    (value.publicServiceNumber === undefined || value.publicServiceNumber === null || typeof value.publicServiceNumber === 'string') &&
    (value.originName === undefined || value.originName === null || typeof value.originName === 'string') &&
    (value.destinationName === undefined || value.destinationName === null || typeof value.destinationName === 'string') &&
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
    operationalArrivalTime: localTime(item.operationalArrivalAt),
    delay: { kind: 'unknown' },
    cancellation: { kind: 'known', value: item.status === 'Cancelled' },
    ...(item.updatedAt ? { updateTime: { displayValue: new Intl.DateTimeFormat('en-LK', { timeZone: 'Asia/Colombo', dateStyle: 'medium', timeStyle: 'short' }).format(new Date(item.updatedAt)), meaning: 'authoritative-verified' as const } } : {}),
  }
}

export function createStationDepartureAdapter(
  client: Pick<HttpClient, 'get'>,
  now: () => Date = () => new Date(),
  refreshIntervalMs = DEFAULT_STATION_DEPARTURE_REFRESH_INTERVAL_MS,
): StationDepartureSource {
  if (!Number.isSafeInteger(refreshIntervalMs) || refreshIntervalMs < 1 || refreshIntervalMs > 2_147_483_647) {
    throw new RangeError('Refresh interval must be a positive safe browser timer interval.')
  }

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
        const callType = item.scheduledDepartureAt || item.operationalDepartureAt ? 'departure' : 'arrival'
        const scheduled = localTime(callType === 'departure' ? item.scheduledDepartureAt : item.scheduledArrivalAt)
        const number = item.publicServiceNumber?.trim()
        const name = item.displayName?.trim()
        return {
          departureId: `${item.journeyInstanceId}:${item.stopOrder}`,
          serviceDate: item.stationLocalDepartureDate,
          serviceLabel: [number, name].filter(Boolean).join(' · ') || item.serviceCode?.trim() || 'Train service',
          routeLabel: item.originName && item.destinationName ? `${item.originName} → ${item.destinationName}` : undefined,
          callType,
          scheduledTime: { displayValue: scheduled.kind === 'known' ? scheduled.value : 'Unknown' },
          operational: mapOperational(item),
          sourceRevision: item.revision,
          identityAmbiguous: item.ambiguity === 'ambiguous',
        }
      }),
    }
  }

  let stationId: string | undefined
  let timer: ReturnType<typeof setInterval> | undefined
  let request: Promise<StationDepartureSnapshot> | undefined
  let disposed = false
  const subscribers = new Set<{
    listener: (snapshot: StationDepartureSnapshot) => void
    handlers?: Parameters<StationDepartureSource['subscribe']>[1]
  }>()

  const startPolling = () => {
    if (disposed || timer || !stationId || subscribers.size === 0) return
    timer = setInterval(() => {
      if (disposed || request || !stationId || (typeof navigator !== 'undefined' && !navigator.onLine)) return
      const requestedStation = stationId
      const current = read(requestedStation)
      request = current
      void current.then(
        (snapshot) => {
          if (disposed || requestedStation !== stationId) return
          for (const subscriber of subscribers) {
            if (subscriber.handlers?.onRevalidated) subscriber.handlers.onRevalidated(snapshot)
            else subscriber.listener(snapshot)
          }
        },
        (error: unknown) => {
          if (disposed) return
          const message = error instanceof Error ? error.message : 'Departure information could not be refreshed.'
          for (const subscriber of subscribers) subscriber.handlers?.onRefreshFailure?.(message)
        },
      ).finally(() => { if (request === current) request = undefined })
    }, refreshIntervalMs)
  }

  const fetch = (id: string) => {
    stationId = id
    startPolling()
    if (request) return request
    const current = read(id)
    request = current
    void current.then(
      () => { if (request === current) request = undefined },
      () => { if (request === current) request = undefined },
    )
    return current
  }

  return {
    getInitialSnapshot: fetch,
    revalidate: (id) => fetch(id),
    subscribe: (listener, handlers) => {
      if (disposed) return () => undefined
      const subscriber = { listener, handlers }
      subscribers.add(subscriber)
      startPolling()
      return () => {
        subscribers.delete(subscriber)
        if (subscribers.size === 0 && timer) {
          clearInterval(timer)
          timer = undefined
        }
      }
    },
    dispose: () => {
      disposed = true
      if (timer) clearInterval(timer)
      timer = undefined
      subscribers.clear()
    },
  }
}
