import type { StationContext } from '../model/display-model'
import { DisplayLanguageSelect, useDisplayLanguage } from '../../../app/language'

export type StationHeaderState =
  | { kind: 'loading' }
  | { kind: 'ready'; station: StationContext }
  | { kind: 'unavailable'; message: string }

interface StationHeaderProps {
  state: StationHeaderState
  stations?: StationContext[]
  onStationChange?: (stationId: string) => void
}

export function StationHeader({ state, stations = [], onStationChange }: StationHeaderProps) {
  const { t } = useDisplayLanguage()
  return (
    <header className="station-display__header">
      {state.kind === 'ready' ? <DisplayLanguageSelect /> : null}
      {state.kind === 'loading' ? (
        <p className="display-state-message" role="status" aria-live="polite">
          {t('Loading station information')}
        </p>
      ) : state.kind === 'unavailable' ? (
        <p className="display-state-message display-state-message--alert" role="alert">
          {t(state.message)}
        </p>
      ) : (
        <h1 className="station-display__station-name">
          {onStationChange && stations.length > 1 ? (
            <span className="station-display__station-select-wrap">
              <select
                className="station-display__station-select"
                aria-label={t('Station')}
                value={state.station.stationId}
                onChange={(event) => onStationChange(event.target.value)}
              >
                {stations.map((station) => <option key={station.stationId} value={station.stationId}>{station.name}</option>)}
              </select>
            </span>
          ) : state.station.name}
        </h1>
      )}
    </header>
  )
}
