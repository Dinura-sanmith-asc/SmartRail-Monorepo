import type { DisplayInformationState } from '../model/display-state'
import { DepartureBoard } from './departure-board'

interface DisplayStateViewProps {
  configuredStationId: string
  state: DisplayInformationState
}

export function DisplayStateView({ configuredStationId, state }: DisplayStateViewProps) {
  const snapshot = 'snapshot' in state ? state.snapshot : undefined
  const status = (() => {
    switch (state.kind) {
      case 'loading': return 'Loading departure information.'
      case 'unavailable': return 'Departure information is unavailable.'
      case 'error': return state.message
      case 'disconnected': return 'Network connection is unavailable. Departure information may be outdated.'
      case 'stale': return 'Departure information may be outdated.'
      case 'revalidating': return 'Checking departure information. Displayed information may be outdated.'
      case 'recovered': return 'Departure information has been revalidated.'
      case 'loaded':
      case 'empty': return ''
    }
  })()
  const updateTime = snapshot?.departures.find((departure) => departure.operational.updateTime?.meaning === 'authoritative-verified')?.operational.updateTime

  return (
    <>
      {status ? <p className={`display-state-message${state.kind === 'error' || state.kind === 'unavailable' ? ' display-state-message--alert' : ''}`} role={state.kind === 'error' || state.kind === 'unavailable' ? 'alert' : 'status'} aria-live="polite">{status}</p> : null}
      {updateTime ? <p className="display-state-message display-state-message--timestamp">Last updated: {updateTime.displayValue}</p> : null}
      {snapshot ? <DepartureBoard configuredStationId={configuredStationId} snapshot={snapshot} /> : null}
    </>
  )
}
