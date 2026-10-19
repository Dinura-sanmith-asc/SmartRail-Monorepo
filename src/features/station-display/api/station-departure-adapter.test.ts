import { describe, expect, it, vi } from 'vitest'
import { HttpClientError } from '../../../shared/api/http-client'
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
})
