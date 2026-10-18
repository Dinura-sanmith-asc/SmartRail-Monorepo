import { describe, expect, it, vi } from 'vitest'
import { HttpClientError } from '../../../shared/api/http-client'
import { findPublishedStation } from './station-reference-client'

const configuredStationId = '00000000-0000-4000-8000-000000000001'
const otherStationId = '00000000-0000-4000-8000-000000000002'

describe('published station lookup', () => {
  it('returns only the exact configured published station', async () => {
    const get = vi.fn().mockResolvedValue({
      items: [
        { stationId: otherStationId, name: 'Other Test Station' },
        { stationId: configuredStationId, name: 'Authoritative Test Station' },
      ],
    })

    await expect(findPublishedStation(configuredStationId, { get })).resolves.toEqual({
      kind: 'found',
      station: { stationId: configuredStationId, name: 'Authoritative Test Station' },
    })
    expect(get).toHaveBeenCalledWith('/stations')
  })

  it('does not fall back when the configured station is not published', async () => {
    const get = vi.fn().mockResolvedValue({
      items: [{ stationId: otherStationId, name: 'Other Test Station' }],
    })

    await expect(findPublishedStation(configuredStationId, { get })).resolves.toEqual({ kind: 'unpublished' })
  })

  it('reports API failures as unavailable station information', async () => {
    const get = vi.fn().mockRejectedValue(new HttpClientError('network', 'unavailable'))

    await expect(findPublishedStation(configuredStationId, { get })).resolves.toEqual({
      kind: 'unavailable',
      failure: 'network',
    })
  })

  it('treats a response outside the published StationList contract as unavailable', async () => {
    const get = vi.fn().mockResolvedValue({ items: [{ stationId: configuredStationId }] })

    await expect(findPublishedStation(configuredStationId, { get })).resolves.toEqual({
      kind: 'unavailable',
      failure: 'malformed-response',
    })
  })
})
