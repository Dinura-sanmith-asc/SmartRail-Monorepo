import type { StationDepartureSnapshot } from '../../model/display-model'
import type { StationDepartureSource } from '../station-departure-source'

export interface FixtureDepartureSource extends StationDepartureSource {
  /** Test/development control for delivering an update; this is not a live transport. */
  publish(snapshot: StationDepartureSnapshot): void
}

export function createFixtureDepartureSource(
  fixtures: readonly StationDepartureSnapshot[],
): FixtureDepartureSource {
  const listeners = new Set<(snapshot: StationDepartureSnapshot) => void>()
  const getSnapshot = (stationId: string): StationDepartureSnapshot => {
    const snapshot = fixtures.find((item) => item.station.stationId === stationId)
    if (!snapshot) throw new Error(`No fixture exists for station ID: ${stationId}`)
    return snapshot
  }

  return {
    async getInitialSnapshot(stationId) {
      return getSnapshot(stationId)
    },
    async revalidate(stationId) {
      return getSnapshot(stationId)
    },
    subscribe(listener) {
      listeners.add(listener)
      return () => listeners.delete(listener)
    },
    publish(snapshot) {
      if (snapshot.origin !== 'fixture') {
        throw new Error('Fixture source cannot publish authoritative data.')
      }
      for (const listener of listeners) listener(snapshot)
    },
  }
}
