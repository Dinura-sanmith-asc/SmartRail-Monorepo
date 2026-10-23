import type { StationDepartureSnapshot } from '../model/display-model'
import { DepartureRow } from './departure-row'
import { useDisplayLanguage } from '../../../app/language'

interface DepartureBoardProps {
  configuredStationId: string
  snapshot: StationDepartureSnapshot
}

export function DepartureBoard({ configuredStationId, snapshot }: DepartureBoardProps) {
  const { t } = useDisplayLanguage()
  const isFixture = snapshot.origin === 'fixture'

  return (
    <section aria-labelledby="departure-heading" className="departure-board">
      {isFixture ? (
        <p className="departure-board__demo-label" role="note">
          {t('DEMO DATA — NOT LIVE')}
        </p>
      ) : null}
      <h2 id="departure-heading" className="departure-board__title">{t('Arrivals and departures')}</h2>
      {snapshot.station.stationId !== configuredStationId ? (
        <p role="alert" className="display-state-message display-state-message--alert">
          {t('Departure information is unavailable for this station.')}
        </p>
      ) : snapshot.departures.length === 0 ? (
        <p className="display-state-message" role="status" aria-live="polite">
          {t(isFixture ? 'No fixture departures are available for this station.' : 'No arrivals or departures are currently available.')}
        </p>
      ) : (
        <ul className="departure-board__list" aria-label={t('Station departures')}>
          {snapshot.departures.map((departure) => (
            <DepartureRow departure={departure} key={departure.departureId} />
          ))}
        </ul>
      )}
    </section>
  )
}
