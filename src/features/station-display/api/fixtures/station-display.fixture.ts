import type { StationContext, StationDepartureSnapshot } from '../../model/display-model'

/** Synthetic examples for development and tests only. They are not railway information. */
export const DEMO_STATION_IDS = {
  central: '10000000-0000-0000-0000-000000000001', // Colombo Fort
  quiet: '00000000-0000-4000-8000-000000000102',
  harbor: '10000000-0000-0000-0000-000000000002', // Galle
} as const

export const stationDisplayFixtures: readonly StationDepartureSnapshot[] = [
  {
    station: { stationId: DEMO_STATION_IDS.central, name: 'DEMO Central Station' },
    origin: 'fixture',
    departures: [
      {
        departureId: 'demo-central-2026-10-18-a',
        serviceDate: '2026-10-18',
        serviceLabel: 'DEMO TEST Service A',
        scheduledTime: { displayValue: '08:15' },
        operational: {
          status: { kind: 'known', value: 'DEMO delayed' },
          platform: { kind: 'known', value: 'DEMO platform 2' },
          delay: { kind: 'known', value: 'DEMO delay example' },
          cancellation: { kind: 'known', value: false },
        },
      },
      {
        departureId: 'demo-central-2026-10-18-b',
        serviceDate: '2026-10-18',
        serviceLabel: 'DEMO TEST Service B',
        scheduledTime: { displayValue: '08:40' },
        operational: {
          status: { kind: 'known', value: 'DEMO cancelled' },
          platform: { kind: 'unknown' },
          delay: { kind: 'unavailable' },
          cancellation: { kind: 'known', value: true },
        },
      },
    ],
  },
  {
    station: { stationId: DEMO_STATION_IDS.quiet, name: 'DEMO Quiet Station' },
    origin: 'fixture',
    departures: [],
  },
  {
    station: { stationId: DEMO_STATION_IDS.harbor, name: 'DEMO Harbor Station' },
    origin: 'fixture',
    departures: [
      {
        departureId: 'demo-harbor-2026-10-18-a',
        serviceDate: '2026-10-18',
        serviceLabel: 'DEMO TEST Service C',
        scheduledTime: { displayValue: '09:05' },
        operational: {
          status: { kind: 'unknown' },
          platform: { kind: 'known', value: 'DEMO platform change example' },
          delay: { kind: 'unknown' },
          cancellation: { kind: 'unknown' },
        },
      },
    ],
  },
]

export function getStationFixture(stationId: string): StationDepartureSnapshot | undefined {
  return stationDisplayFixtures.find((fixture) => fixture.station.stationId === stationId)
}

export function getFixtureStation(stationId: string): StationContext | undefined {
  return getStationFixture(stationId)?.station
}
