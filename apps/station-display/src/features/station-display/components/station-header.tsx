import type { StationContext } from '../model/display-model'
import { DisplayLanguageSelect, useDisplayLanguage } from '../../../app/language'

export type StationHeaderState =
  | { kind: 'loading' }
  | { kind: 'ready'; station: StationContext }
  | { kind: 'unavailable'; message: string }

interface StationHeaderProps {
  state: StationHeaderState
}

export function StationHeader({ state }: StationHeaderProps) {
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
        <h1 className="station-display__station-name">{state.station.name}</h1>
      )}
    </header>
  )
}
