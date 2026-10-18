import type { StationDepartureSnapshot } from '../model/display-model'
import { DepartureRow } from './departure-row'

interface DepartureBoardProps {
  configuredStationId: string
  snapshot: StationDepartureSnapshot
}

export function DepartureBoard({ configuredStationId, snapshot }: DepartureBoardProps) {
  const isFixture = snapshot.origin === 'fixture'

  return (
    <section aria-labelledby="departure-heading" className="w-full flex-1 px-8 py-7 text-white">
      {isFixture ? (
        <p className="mb-6 inline-flex rounded bg-amber-300 px-4 py-2 text-lg font-extrabold text-slate-950" role="note">
          DEMO DATA — NOT LIVE
        </p>
      ) : null}
      <h2 id="departure-heading" className="mb-5 text-2xl font-semibold">Departures</h2>
      {snapshot.station.stationId !== configuredStationId ? (
        <p role="alert" className="text-lg text-amber-200">
          Departure information is unavailable for this station.
        </p>
      ) : snapshot.departures.length === 0 ? (
        <p className="text-lg text-slate-200" role="status" aria-live="polite">
          {isFixture ? 'No fixture departures are available for this station.' : 'No departures are currently available.'}
        </p>
      ) : (
        <ul className="space-y-4" aria-label="Station departures">
          {snapshot.departures.map((departure) => (
            <DepartureRow departure={departure} key={departure.departureId} />
          ))}
        </ul>
      )}
    </section>
  )
}
