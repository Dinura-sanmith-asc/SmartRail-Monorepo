import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiRequestError } from '../../src/shared/api/fetchJson'
import { operationsApi } from '../../src/features/operations/operationsApi'

const options = { apiBaseUrl: 'https://api.example.test/', accessToken: 'short-lived-access-token' }
afterEach(() => vi.unstubAllGlobals())

describe('operations API contract mapping', () => {
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
})
