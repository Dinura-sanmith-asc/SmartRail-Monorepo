import { ApiRequestError, fetchJson } from '../../shared/api/fetchJson'

export type Station = { stationId: string; name: string; locationDetails?: string[]; serviceDetails?: string[] }
export type Departure = { journeyInstanceId: string; serviceId: string; operationDate: string; stationLocalDepartureDate: string; revision: number; status: string; stationId: string; stationName: string; stopOrder: number; scheduledDepartureAt: string | null; operationalDepartureAt: string | null; platform: string | null; source: string; updatedAt: string; ambiguity: string }
export type JourneyStop = { journeyStopId: string; stopOrder: number; stationId: string; stationName: string; plannedArrivalAt: string | null; plannedDepartureAt: string | null; operationalArrivalAt: string | null; operationalDepartureAt: string | null; platform: string | null; ambiguity: string }
export type Journey = { journeyInstanceId: string; serviceId: string; operationDate: string; revision: number; status: string; stops: JourneyStop[]; source: string; publishedAt: string | null; lastUpdatedAt: string | null; eta: { value: string; updatedAt: string; freshness: string } | null }
export type JourneyUpdate = { updateId: string; revision: number; kind: string; status: string; source: string; publishedAt: string; publicChange: Record<string, unknown> }
export type DriverObservation = { observationId: string; journeyInstanceId: string; assignmentId: string; driverSubject: string; text: string; reportedAtUtc: string; journeyRevision: number }
export type DriverAssignment = { assignmentId: string; driverSubject: string; journey: { snapshot: Journey; updates: JourneyUpdate[] } }
export type IncidentAssociation = { associationId: string; kind: string; resourceId: string }
export type Incident = { incidentId: string; categoryCode: string; description: string; state: string; revision: number; reportedAt: string; occurredAt: string | null; associations: IncidentAssociation[] }
type Options = { apiBaseUrl: string; accessToken: string | null; signal?: AbortSignal }

async function request<T>(path: string, options: Options, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  headers.set('Accept', 'application/json')
  if (init.body) headers.set('Content-Type', 'application/json')
  if (options.accessToken) headers.set('Authorization', `Bearer ${options.accessToken}`)
  return fetchJson<T>(`${options.apiBaseUrl.replace(/\/$/, '')}${path}`, { ...init, headers, signal: options.signal ?? init.signal })
}

export const operationsApi = {
  stations: (o: Options) => request<{ items: Station[] }>('/api/stations', o),
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
  if (!(error instanceof ApiRequestError)) return 'The request could not be completed. Try again later.'
  const labels = { unauthenticated: 'Your session has expired. Sign in again to continue.', forbidden: 'The server denied this operation for your role or resource scope.', 'authority-unavailable': 'An authorization or operational data source is temporarily unavailable. Try again shortly.', 'service-error': 'The request could not be completed. Try again later.', timeout: 'The request took too long. Try again.', offline: 'A network connection is unavailable. Check your connection and try again.', 'malformed-response': 'The service returned an unexpected response.' }
  return labels[error.failure]
}
