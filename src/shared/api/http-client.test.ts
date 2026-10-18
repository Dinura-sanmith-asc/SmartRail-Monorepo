import { describe, expect, it, vi } from 'vitest'
import { createHttpClient } from './http-client'

describe('HTTP client', () => {
  it('returns typed JSON for a successful response', async () => {
    const fetchImpl = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ stationId: 'demo-station' }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    )
    const client = createHttpClient({ baseUrl: 'https://rail.example/api/', fetchImpl })

    await expect(client.get<{ stationId: string }>('/stations')).resolves.toEqual({
      stationId: 'demo-station',
    })
    expect(fetchImpl).toHaveBeenCalledWith('https://rail.example/api/stations', {
      method: 'GET',
      headers: { Accept: 'application/json, application/problem+json' },
    })
  })

  it('distinguishes network failures', async () => {
    const client = createHttpClient({
      baseUrl: 'https://rail.example',
      fetchImpl: vi.fn().mockRejectedValue(new TypeError('offline')),
    })

    await expect(client.get('/stations')).rejects.toMatchObject({
      kind: 'network',
      message: 'The request could not reach the server.',
    })
  })

  it('distinguishes malformed successful JSON', async () => {
    const client = createHttpClient({
      baseUrl: 'https://rail.example',
      fetchImpl: vi.fn().mockResolvedValue(new Response('{', { status: 200 })),
    })

    await expect(client.get('/stations')).rejects.toMatchObject({ kind: 'malformed-response' })
  })

  it('uses the checked-in Problem contract for HTTP failures', async () => {
    const client = createHttpClient({
      baseUrl: 'https://rail.example',
      fetchImpl: vi.fn().mockResolvedValue(
        new Response(JSON.stringify({
          type: 'about:blank',
          title: 'Not Found',
          status: 404,
          code: 'station_not_found',
        }), { status: 404, headers: { 'Content-Type': 'application/problem+json' } }),
      ),
    })

    await expect(client.get('/stations/missing')).rejects.toMatchObject({
      kind: 'http',
      status: 404,
      problem: { title: 'Not Found', code: 'station_not_found' },
    })
  })

  it('reports an invalid Problem response as malformed, without exposing its body', async () => {
    const client = createHttpClient({
      baseUrl: 'https://rail.example',
      fetchImpl: vi.fn().mockResolvedValue(new Response('{ private body', { status: 500 })),
    })

    await expect(client.get('/stations')).rejects.toMatchObject({ kind: 'malformed-response' })
  })
})
