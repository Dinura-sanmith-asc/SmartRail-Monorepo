import type { StationDepartureSnapshot } from '../model/display-model'
import { DepartureRow } from './departure-row'

interface DepartureBoardProps {
  configuredStationId: string
  snapshot: StationDepartureSnapshot
}

export function DepartureBoard({ configuredStationId, snapshot }: DepartureBoardProps) {
  const isFixture = snapshot.origin === 'fixture'

  return (
    <section aria-labelledby="departure-heading" className="departure-board">
      {isFixture ? (
        <p className="departure-board__demo-label" role="note">
          DEMO DATA — NOT LIVE
        </p>
      ) : null}
      <h2 id="departure-heading" className="departure-board__title">Departures</h2>
      {snapshot.station.stationId !== configuredStationId ? (
        <p role="alert" className="display-state-message display-state-message--alert">
          Departure information is unavailable for this station.
        </p>
      ) : snapshot.departures.length === 0 ? (
        <p className="display-state-message" role="status" aria-live="polite">
          {isFixture ? 'No fixture departures are available for this station.' : 'No departures are currently available.'}
        </p>
      ) : (
        <ul className="departure-board__list" aria-label="Station departures">
          {snapshot.departures.map((departure) => (
            <DepartureRow departure={departure} key={departure.departureId} />
          ))}
        </ul>
      )}
    </section>
  )
}
