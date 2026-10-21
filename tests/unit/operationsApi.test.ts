import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiRequestError } from '../../src/shared/api/fetchJson'
import { operationsApi, parseOperationsOverview } from '../../src/features/operations/operationsApi'

const options = { apiBaseUrl: 'https://api.example.test/', accessToken: 'short-lived-access-token' }
const overview = {
  operationalDate: '2026-11-02',
  metrics: { totalScheduled: 4, running: 1, delayed: 2, openIncidents: 3, cancelled: 1, completed: 1 },
  journeys: [{
    journeyId: 'journey-1', serviceId: 'internal-service-id', serviceCode: 'SRL-EXAMPLE',
    serviceDisplayName: 'Backend Service Name', publicServiceNumber: 'PUBLIC-42', routeCode: 'R-1',
    routeName: 'Origin to Destination', operationDate: '2026-11-02', origin: 'Origin', destination: 'Destination',
    scheduledDeparture: '2026-11-02T08:00:00+05:30', scheduledArrival: '2026-11-02T12:00:00+05:30',
    status: 'Running', recordedDelayMinutes: 12, lastReportedStation: 'Current reported stop',
    lastUpdateAt: '2026-11-02T03:00:00Z', assignedDriver: null,
  }],
}
afterEach(() => vi.unstubAllGlobals())

describe('operations API contract mapping', () => {
  it('maps the approved overview DTO, forwards Cognito auth, and requests a global date without station scope', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify(overview), { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)

    const result = await operationsApi.operationsOverview('2026-11-02', options)

    expect(result).toEqual(overview)
    expect(fetchMock).toHaveBeenCalledWith('https://api.example.test/api/operations/overview?date=2026-11-02', expect.objectContaining({ headers: expect.any(Headers) }))
    const request = fetchMock.mock.calls[0]?.[1] as RequestInit
    expect(new Headers(request.headers).get('Authorization')).toBe('Bearer short-lived-access-token')
  })

  it('rejects malformed overview DTOs instead of rendering untrusted values', () => {
    expect(() => parseOperationsOverview({ ...overview, metrics: { ...overview.metrics, delayed: -1 } })).toThrow(/invalid operations overview/i)
    expect(() => parseOperationsOverview({ ...overview, journeys: [{ ...overview.journeys[0], status: 'Moving' }] })).toThrow(/invalid operations overview/i)
  })

  it('classifies malformed overview payloads as safe response errors', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ operationalDate: '2026-11-02', metrics: {}, journeys: [] }), { status: 200 })))
    await expect(operationsApi.operationsOverview('2026-11-02', options)).rejects.toMatchObject<ApiRequestError>({ failure: 'malformed-response' })
  })

  it('uses the configured API base and existing Cognito bearer token for protected reads', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ items: [] }), { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)

    await operationsApi.incidents(options)

    expect(fetchMock).toHaveBeenCalledWith('https://api.example.test/api/incidents', expect.objectContaining({
      headers: expect.any(Headers),
    }))
    const request = fetchMock.mock.calls[0]?.[1] as RequestInit
    expect(new Headers(request.headers).get('Authorization')).toBe('Bearer short-lived-access-token')
  })

  it('preserves server authorization denial as a safe 403 failure', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('', { status: 403 })))

    await expect(operationsApi.incidents(options)).rejects.toMatchObject<ApiRequestError>({ status: 403, failure: 'forbidden' })
  })

  it('uses revision and idempotency fields in approved write requests', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ ok: true }), { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)

    await operationsApi.publishStatus('journey-id', { expectedJourneyRevision: 4, status: 'Delayed' }, options)

    expect(fetchMock).toHaveBeenCalledWith('https://api.example.test/api/operations/journeys/journey-id/status', expect.objectContaining({
      method: 'PATCH',
      body: JSON.stringify({ expectedJourneyRevision: 4, status: 'Delayed' }),
    }))
  })

  it('submits driver progress to the assigned journey with a unique request ID', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ observationId: 'observation-1' }), { status: 201 }))
    vi.stubGlobal('fetch', fetchMock)

    await operationsApi.submitDriverObservation('journey-1', 'Arrived at stop', options)

    const [url, request] = fetchMock.mock.calls[0] as [string, RequestInit]
    expect(url).toBe('https://api.example.test/api/driver/journeys/journey-1/observations')
    expect(request.method).toBe('POST')
    expect(JSON.parse(String(request.body))).toMatchObject({ text: 'Arrived at stop' })
    expect(JSON.parse(String(request.body)).requestId).toMatch(/^[0-9a-f-]{36}$/i)
  })

  it('creates, patches, and deletes stations through the authenticated railway API', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response(JSON.stringify({ stationId: 'server-uuid', name: 'Harbor' }), { status: 201 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ stationId: 'server-uuid', name: 'Harbor Central' }), { status: 200 }))
      .mockResolvedValueOnce(new Response(null, { status: 204 }))
    vi.stubGlobal('fetch', fetchMock)

    expect(await operationsApi.createStation('Harbor', options)).toMatchObject({ stationId: 'server-uuid' })
    expect(await operationsApi.updateStation('server-uuid', 'Harbor Central', options)).toMatchObject({ name: 'Harbor Central' })
    await operationsApi.deleteStation('server-uuid', options)
    expect(fetchMock.mock.calls.map(([url, init]) => [url, (init as RequestInit).method])).toEqual([
      ['https://api.example.test/api/stations', 'POST'],
      ['https://api.example.test/api/stations/server-uuid', 'PATCH'],
      ['https://api.example.test/api/stations/server-uuid', 'DELETE'],
    ])
    const createInit = fetchMock.mock.calls[0]?.[1] as RequestInit
    expect(JSON.parse(String(createInit.body))).toEqual({ name: 'Harbor' })
    expect(new Headers(createInit.headers).get('Authorization')).toBe('Bearer short-lived-access-token')
  })

  it('retains backend problem titles for station conflict feedback', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ title: 'Station is referenced by railway data.' }), { status: 409 })))
    await expect(operationsApi.deleteStation('station-1', options)).rejects.toMatchObject<ApiRequestError>({ backendMessage: 'Station is referenced by railway data.' })
  })

  it('includes field errors from backend problem responses', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ title: 'Station data is invalid.', errors: { name: ['Station name is required.'] } }), { status: 400 })))
    await expect(operationsApi.createStation('', options)).rejects.toMatchObject<ApiRequestError>({ backendMessage: 'Station data is invalid. Station name is required.' })
  })
})
