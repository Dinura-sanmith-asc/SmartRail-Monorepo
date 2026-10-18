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
      {status ? <p className="mx-8 mt-6 text-lg text-amber-200" role={state.kind === 'error' || state.kind === 'unavailable' ? 'alert' : 'status'}>{status}</p> : null}
      {updateTime ? <p className="mx-8 mt-2 text-sm text-slate-300">Last updated: {updateTime.displayValue}</p> : null}
      {snapshot ? <DepartureBoard configuredStationId={configuredStationId} snapshot={snapshot} /> : null}
    </>
  )
}
