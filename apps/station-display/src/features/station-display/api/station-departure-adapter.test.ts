import { afterEach, describe, expect, it, vi } from 'vitest'
import { HttpClientError, type HttpClient } from '../../../shared/api/http-client'
import { createStationDepartureAdapter } from './station-departure-adapter'

const stationId = '00000000-0000-4000-8000-000000000101'
const serviceId = '00000000-0000-4000-8000-000000000201'
const journeyId = '00000000-0000-4000-8000-000000000301'
const departure = {
  journeyInstanceId: journeyId,
  serviceId,
  operationDate: '2026-10-17',
  stationLocalDepartureDate: '2026-10-18',
  revision: 3,
  status: 'Delayed',
  stationId,
  stationName: 'Contract Station',
  stopOrder: 2,
  scheduledDepartureAt: '2026-10-17T19:15:00Z',
  operationalDepartureAt: '2026-10-17T19:35:00Z',
  platform: '4',
  source: 'ControllerManual',
  updatedAt: '2026-10-17T19:00:00Z',
  ambiguity: 'ambiguous',
}

describe('approved Journeys & Status station departures adapter', () => {
  afterEach(() => vi.useRealTimers())

  it('requests the Colombo local date and maps only published projection values', async () => {
    const get = vi.fn()
      .mockResolvedValueOnce({ items: [{ stationId, name: 'Contract Station' }] })
      .mockResolvedValueOnce([departure])
    const source = createStationDepartureAdapter({ get }, () => new Date('2026-10-17T18:30:00Z'))

    const snapshot = await source.getInitialSnapshot(stationId)

    expect(get).toHaveBeenNthCalledWith(1, '/stations')
    expect(get).toHaveBeenNthCalledWith(2, `/stations/${stationId}/departures?date=2026-10-18`)
    expect(snapshot).toMatchObject({
      origin: 'authoritative',
      station: { stationId, name: 'Contract Station' },
      departures: [{
        departureId: `${journeyId}:2`,
        serviceDate: '2026-10-18',
        serviceLabel: `Service ID ${serviceId}`,
        scheduledTime: { displayValue: '00:45' },
        sourceRevision: 3,
        identityAmbiguous: true,
        operational: {
          status: { kind: 'known', value: 'Delayed' },
          platform: { kind: 'known', value: '4' },
          operationalDepartureTime: { kind: 'known', value: '01:05' },
          delay: { kind: 'unknown' },
          cancellation: { kind: 'known', value: false },
          updateTime: { meaning: 'unverified' },
        },
      }],
    })
  })

  it('preserves an empty array and missing schedule values as unknown', async () => {
    const get = vi.fn()
      .mockResolvedValueOnce({ items: [{ stationId, name: 'Contract Station' }] })
      .mockResolvedValueOnce([{
        ...departure,
        stationLocalDepartureDate: '2026-10-17',
        scheduledDepartureAt: null,
        operationalDepartureAt: null,
        platform: null,
        status: 'Cancelled',
        ambiguity: 'none',
      }])
    const source = createStationDepartureAdapter({ get }, () => new Date('2026-10-17T12:00:00Z'))
    await expect(source.getInitialSnapshot(stationId)).resolves.toMatchObject({
      origin: 'authoritative',
      departures: [{
        scheduledTime: { displayValue: 'Unknown' },
        operational: {
          operationalDepartureTime: { kind: 'unknown' },
          platform: { kind: 'unknown' },
          delay: { kind: 'unknown' },
          cancellation: { kind: 'known', value: true },
        },
      }],
    })

    const emptyGet = vi.fn()
      .mockResolvedValueOnce({ items: [{ stationId, name: 'Contract Station' }] })
      .mockResolvedValueOnce([])
    await expect(createStationDepartureAdapter({ get: emptyGet }, () => new Date('2026-10-17T12:00:00Z'))
      .getInitialSnapshot(stationId)).resolves.toMatchObject({ origin: 'authoritative', departures: [] })
  })

  it('rejects payloads outside the published contract or for another date/station', async () => {
    const get = vi.fn()
      .mockResolvedValueOnce({ items: [{ stationId, name: 'Contract Station' }] })
      .mockResolvedValueOnce([{ ...departure, stationId: '00000000-0000-4000-8000-000000000999' }])
    const source = createStationDepartureAdapter({ get }, () => new Date('2026-10-17T18:30:00Z'))
    await expect(source.getInitialSnapshot(stationId)).rejects.toMatchObject({ kind: 'malformed-response' } as Partial<HttpClientError>)
  })

  it('polls once per configured interval, keeps station scope, reports failures and stops after unsubscribe', async () => {
    vi.useFakeTimers()
    const get = vi.fn()
      .mockResolvedValueOnce({ items: [{ stationId, name: 'Contract Station' }] })
      .mockResolvedValueOnce([departure])
      .mockResolvedValueOnce({ items: [{ stationId, name: 'Contract Station' }] })
      .mockResolvedValueOnce([departure])
      .mockResolvedValueOnce({ items: [{ stationId, name: 'Contract Station' }] })
      .mockRejectedValueOnce(new Error('offline'))
    const source = createStationDepartureAdapter({ get }, () => new Date('2026-10-17T18:30:00Z'), 1_000)
    const updated = vi.fn()
    const failed = vi.fn()
    const unsubscribe = source.subscribe(vi.fn(), { onRevalidated: updated, onRefreshFailure: failed })

    await source.getInitialSnapshot(stationId)
    await vi.advanceTimersByTimeAsync(1_000)
    expect(updated).toHaveBeenCalledWith(expect.objectContaining({ station: { stationId, name: 'Contract Station' } }))
    expect(failed).not.toHaveBeenCalled()

    await vi.advanceTimersByTimeAsync(1_000)
    expect(failed).toHaveBeenCalledWith('offline')
    unsubscribe()
    await vi.advanceTimersByTimeAsync(5_000)
    expect(get).toHaveBeenCalledTimes(6) // each read validates the station and then reads departures
    source.dispose?.()
  })

  it('does not overlap polls and recomputes the Colombo date after midnight', async () => {
    vi.useFakeTimers()
    let currentTime = new Date('2026-10-17T18:29:59Z') // 23:59:59 in Colombo
    const pending: Array<(value: unknown) => void> = []
    const get = vi.fn(<T>(path: string): Promise<T> => {
      if (path === '/stations') return Promise.resolve({ items: [{ stationId, name: 'Contract Station' }] } as T)
      return new Promise<T>((resolve) => pending.push((value) => resolve(value as T)))
    })
    const source = createStationDepartureAdapter({ get: get as unknown as HttpClient['get'] }, () => currentTime, 1_000)
    const unsubscribe = source.subscribe(vi.fn())
    const initial = source.getInitialSnapshot(stationId)
    await vi.waitFor(() => expect(get).toHaveBeenLastCalledWith('/stations/00000000-0000-4000-8000-000000000101/departures?date=2026-10-17'))
    pending.shift()?.([{ ...departure, operationDate: '2026-10-17', stationLocalDepartureDate: '2026-10-17' }])
    await initial

    currentTime = new Date('2026-10-17T18:30:00Z')
    await vi.advanceTimersByTimeAsync(1_000)
    expect(get).toHaveBeenLastCalledWith('/stations/00000000-0000-4000-8000-000000000101/departures?date=2026-10-18')
    // Let this request remain pending while another interval elapses.
    const reconnectRevalidation = source.revalidate(stationId)
    await vi.advanceTimersByTimeAsync(3_000)
    expect(get.mock.calls.filter(([path]) => String(path).includes('/departures?')).length).toBe(2)
    pending.shift()?.([{ ...departure, operationDate: '2026-10-17', stationLocalDepartureDate: '2026-10-18' }])
    await reconnectRevalidation
    await vi.advanceTimersByTimeAsync(1_000)
    expect(get).toHaveBeenLastCalledWith('/stations/00000000-0000-4000-8000-000000000101/departures?date=2026-10-18')
    pending.shift()?.([{ ...departure, operationDate: '2026-10-17', stationLocalDepartureDate: '2026-10-18' }])
    unsubscribe()
    source.dispose?.()
  })

  it('validates the interval defensively and disposes its timer', async () => {
    expect(() => createStationDepartureAdapter({ get: vi.fn() }, undefined, 0)).toThrow(RangeError)
    vi.useFakeTimers()
    const get = vi.fn()
      .mockResolvedValueOnce({ items: [{ stationId, name: 'Contract Station' }] })
      .mockResolvedValue([departure])
    const source = createStationDepartureAdapter({ get }, () => new Date('2026-10-17T18:30:00Z'), 1_000)
    source.subscribe(vi.fn())
    await source.getInitialSnapshot(stationId)
    source.dispose?.()
    await vi.advanceTimersByTimeAsync(10_000)
    expect(get).toHaveBeenCalledTimes(2)
  })
})
