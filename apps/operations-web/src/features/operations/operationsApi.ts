import { ApiRequestError, fetchJson } from '../../shared/api/fetchJson'

export type Station = { stationId: string; name: string; locationDetails?: string[]; serviceDetails?: string[] }
export type ManagedTrainService = { serviceId: string; serviceCode: string; displayName: string | null; publicServiceNumber: string | null; routeId: string; routeCode: string; routeName: string; isPublished: boolean; routeStopIds: string[] }
export type ManagedRouteStop = { routeStopId: string; stationId: string; stationName: string; stopOrder: number }
export type ManagedRoute = { routeId: string; routeCode: string; name: string; isPublished: boolean; stops: ManagedRouteStop[] }
export type Departure = { journeyInstanceId: string; serviceId: string; operationDate: string; stationLocalDepartureDate: string; revision: number; status: string; stationId: string; stationName: string; stopOrder: number; scheduledDepartureAt: string | null; operationalDepartureAt: string | null; platform: string | null; source: string; updatedAt: string; ambiguity: string }
export type JourneyStop = { journeyStopId: string; stopOrder: number; stationId: string; stationName: string; plannedArrivalAt: string | null; plannedDepartureAt: string | null; operationalArrivalAt: string | null; operationalDepartureAt: string | null; platform: string | null; ambiguity: string }
export type Journey = { journeyInstanceId: string; serviceId: string; operationDate: string; revision: number; status: string; stops: JourneyStop[]; source: string; publishedAt: string | null; lastUpdatedAt: string | null; eta: { value: string; updatedAt: string; freshness: string } | null }
export type JourneyUpdate = { updateId: string; revision: number; kind: string; status: string; source: string; publishedAt: string; publicChange: Record<string, unknown> }
export type DriverObservation = { observationId: string; journeyInstanceId: string; assignmentId: string; driverSubject: string; text: string; reportedAtUtc: string; journeyRevision: number }
export type DriverAssignment = { assignmentId: string; driverSubject: string; journey: { snapshot: Journey; updates: JourneyUpdate[] } }
export type IncidentAssociation = { associationId: string; kind: string; resourceId: string }
export type Incident = { incidentId: string; categoryCode: string; description: string; state: string; revision: number; reportedAt: string; occurredAt: string | null; associations: IncidentAssociation[] }
export type OperationsApiOptions = { apiBaseUrl: string; accessToken: string | null; signal?: AbortSignal }

type Options = OperationsApiOptions

export type OperationsOverviewMetrics = {
  totalScheduled: number
  running: number
  delayed: number
  openIncidents: number
  cancelled: number
  completed: number
}

export type OperationsOverviewJourney = {
  journeyId: string
  serviceId: string
  serviceCode: string
  serviceDisplayName: string | null
  publicServiceNumber: string | null
  routeCode: string
  routeName: string
  operationDate: string
  origin: string
  destination: string
  scheduledDeparture: string | null
  scheduledArrival: string | null
  status: 'Scheduled' | 'Running' | 'Delayed' | 'Interrupted' | 'Cancelled' | 'Terminated' | 'Completed'
  recordedDelayMinutes: number | null
  lastReportedStation: string | null
  lastUpdateAt: string
  assignedDriver: string | null
}

export type OperationsOverview = {
  operationalDate: string
  metrics: OperationsOverviewMetrics
  journeys: OperationsOverviewJourney[]
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

function isDate(value: unknown): value is string {
  if (typeof value !== 'string' || !/^\d{4}-\d{2}-\d{2}$/.test(value)) return false
  const parsed = new Date(`${value}T00:00:00Z`)
  return Number.isFinite(parsed.getTime()) && parsed.toISOString().slice(0, 10) === value
}

function isTimestampOrNull(value: unknown): value is string | null {
  return value === null || (typeof value === 'string' && Number.isFinite(Date.parse(value)))
}

function isStringOrNull(value: unknown): value is string | null {
  return value === null || typeof value === 'string'
}

const journeyStatuses = ['Scheduled', 'Running', 'Delayed', 'Interrupted', 'Cancelled', 'Terminated', 'Completed'] as const

/** Validate the exact public response shape before it reaches dashboard state. */
export function parseOperationsOverview(value: unknown): OperationsOverview {
  if (!isRecord(value) || !isDate(value.operationalDate) || !isRecord(value.metrics) || !Array.isArray(value.journeys)) {
    throw new TypeError('Invalid Operations Overview response')
  }
  const metricKeys = ['totalScheduled', 'running', 'delayed', 'openIncidents', 'cancelled', 'completed'] as const
  const metrics = {} as OperationsOverviewMetrics
  for (const key of metricKeys) {
    const count = value.metrics[key]
    if (typeof count !== 'number' || !Number.isInteger(count) || count < 0) throw new TypeError('Invalid Operations Overview metrics')
    metrics[key] = count
  }
  const journeys = value.journeys.map((item): OperationsOverviewJourney => {
    if (!isRecord(item) || typeof item.journeyId !== 'string' || typeof item.serviceId !== 'string' ||
      typeof item.serviceCode !== 'string' || !isStringOrNull(item.serviceDisplayName) ||
      !isStringOrNull(item.publicServiceNumber) || typeof item.routeCode !== 'string' ||
      typeof item.routeName !== 'string' || !isDate(item.operationDate) || typeof item.origin !== 'string' ||
      typeof item.destination !== 'string' || !isTimestampOrNull(item.scheduledDeparture) ||
      !isTimestampOrNull(item.scheduledArrival) || typeof item.status !== 'string' ||
      !journeyStatuses.includes(item.status as OperationsOverviewJourney['status']) ||
      !(item.recordedDelayMinutes === null || (typeof item.recordedDelayMinutes === 'number' && Number.isFinite(item.recordedDelayMinutes) && item.recordedDelayMinutes >= 0)) ||
      !isStringOrNull(item.lastReportedStation) || !isTimestampOrNull(item.lastUpdateAt) || item.lastUpdateAt === null ||
      !isStringOrNull(item.assignedDriver)) {
      throw new TypeError('Invalid Operations Overview journey')
    }
    return item as OperationsOverviewJourney
  })
  return { operationalDate: value.operationalDate, metrics, journeys }
}

async function request<T>(path: string, options: Options, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  headers.set('Accept', 'application/json')
  if (init.body) headers.set('Content-Type', 'application/json')
  if (options.accessToken) headers.set('Authorization', `Bearer ${options.accessToken}`)
  return fetchJson<T>(`${options.apiBaseUrl.replace(/\/$/, '')}${path}`, { ...init, headers, signal: options.signal ?? init.signal })
}

export const operationsApi = {
  operationsOverview: async (date: string, o: Options) => {
    const response = await request<unknown>(`/api/operations/overview?date=${encodeURIComponent(date)}`, o)
    try { return parseOperationsOverview(response) }
    catch { throw new ApiRequestError('malformed-response') }
  },
  stations: (o: Options) => request<{ items: Station[] }>('/api/stations', o),
  managedServices: (o: Options) => request<ManagedTrainService[]>('/api/services/management', o),
  managedRoutes: (o: Options) => request<ManagedRoute[]>('/api/routes/management', o),
  createRoute: (body: { routeCode: string; name: string; stationIds: string[] }, o: Options) => request<ManagedRoute>('/api/routes/management', o, { method: 'POST', body: JSON.stringify(body) }),
  updateRoute: (id: string, body: { routeCode: string; name: string; stationIds: string[] }, o: Options) => request<ManagedRoute>(`/api/routes/management/${encodeURIComponent(id)}`, o, { method: 'PATCH', body: JSON.stringify(body) }),
  deleteRoute: (id: string, o: Options) => request<void>(`/api/routes/management/${encodeURIComponent(id)}`, o, { method: 'DELETE' }),
  createTrainService: (body: Pick<ManagedTrainService, 'serviceCode' | 'routeId' | 'displayName' | 'publicServiceNumber' | 'routeStopIds'>, o: Options) => request<ManagedTrainService>('/api/services', o, { method: 'POST', body: JSON.stringify(body) }),
  updateTrainService: (id: string, body: Pick<ManagedTrainService, 'serviceCode' | 'routeId' | 'displayName' | 'publicServiceNumber' | 'routeStopIds'>, o: Options) => request<ManagedTrainService>(`/api/services/${encodeURIComponent(id)}`, o, { method: 'PATCH', body: JSON.stringify(body) }),
  deleteTrainService: (id: string, o: Options) => request<void>(`/api/services/${encodeURIComponent(id)}`, o, { method: 'DELETE' }),
  createStation: (name: string, o: Options) => request<Station>('/api/stations', o, { method: 'POST', body: JSON.stringify({ name }) }),
  updateStation: (id: string, name: string, o: Options) => request<Station>(`/api/stations/${encodeURIComponent(id)}`, o, { method: 'PATCH', body: JSON.stringify({ name }) }),
  deleteStation: (id: string, o: Options) => request<void>(`/api/stations/${encodeURIComponent(id)}`, o, { method: 'DELETE' }),
  departures: (stationId: string, date: string, o: Options) => request<Departure[]>(`/api/stations/${encodeURIComponent(stationId)}/departures?date=${encodeURIComponent(date)}`, o),
  journey: (id: string, o: Options) => request<Journey>(`/api/journeys/${encodeURIComponent(id)}`, o),
  journeyUpdates: (id: string, o: Options) => request<JourneyUpdate[]>(`/api/journeys/${encodeURIComponent(id)}/updates`, o),
  driverObservations: (id: string, o: Options) => request<{ items: DriverObservation[] }>(`/api/operations/journeys/${encodeURIComponent(id)}/observations`, o),
  driverAssignments: (o: Options) => request<{ items: DriverAssignment[] }>('/api/driver/assignments', o),
  submitDriverObservation: (id: string, text: string, o: Options) => request<DriverObservation>(`/api/driver/journeys/${encodeURIComponent(id)}/observations`, o, { method: 'POST', body: JSON.stringify({ requestId: crypto.randomUUID(), text }) }),
  incidents: (o: Options) => request<{ items: Incident[] }>('/api/incidents', o),
  createIncident: (body: unknown, o: Options) => request<Incident>('/api/incidents', o, { method: 'POST', headers: { 'Idempotency-Key': crypto.randomUUID() }, body: JSON.stringify(body) }),
  updateIncident: (id: string, body: unknown, o: Options) => request<Incident>(`/api/incidents/${encodeURIComponent(id)}`, o, { method: 'PATCH', headers: { 'Idempotency-Key': crypto.randomUUID() }, body: JSON.stringify(body) }),
  publishStatus: (id: string, body: unknown, o: Options) => request<Journey>(`/api/operations/journeys/${encodeURIComponent(id)}/status`, o, { method: 'PATCH', body: JSON.stringify(body) }),
  updateStop: (id: string, stopId: string, body: unknown, o: Options) => request<Journey>(`/api/operations/journeys/${encodeURIComponent(id)}/stops/${encodeURIComponent(stopId)}`, o, { method: 'PATCH', body: JSON.stringify(body) }),
}

export function apiErrorMessage(error: unknown): string {
  if (error instanceof ApiRequestError && error.backendMessage) return error.backendMessage
  if (!(error instanceof ApiRequestError)) return 'The request could not be completed. Try again later.'
  const labels = { unauthenticated: 'Your session has expired. Sign in again to continue.', forbidden: 'The server denied this operation for your role or resource scope.', 'authority-unavailable': 'An authorization or operational data source is temporarily unavailable. Try again shortly.', 'service-error': 'The request could not be completed. Try again later.', timeout: 'The request took too long. Try again.', offline: 'A network connection is unavailable. Check your connection and try again.', 'malformed-response': 'The service returned an unexpected response.' }
  return labels[error.failure]
}
