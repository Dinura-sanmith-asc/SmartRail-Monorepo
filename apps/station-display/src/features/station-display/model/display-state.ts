import type { StationDepartureSnapshot } from './display-model'

export type DisplayInformationState =
  | { kind: 'loading' }
  | { kind: 'loaded'; snapshot: StationDepartureSnapshot }
  | { kind: 'empty'; snapshot: StationDepartureSnapshot }
  | { kind: 'unavailable' }
  | { kind: 'error'; message: string }
  | { kind: 'disconnected'; snapshot?: StationDepartureSnapshot }
  | { kind: 'stale'; snapshot: StationDepartureSnapshot; message?: string }
  | { kind: 'revalidating'; snapshot?: StationDepartureSnapshot }
  | { kind: 'recovered'; snapshot: StationDepartureSnapshot }

export type DisplayInformationEvent =
  | { type: 'success'; snapshot: StationDepartureSnapshot }
  | { type: 'failure'; message?: string }
  | { type: 'offline' }
  | { type: 'revalidate' }
  | { type: 'revalidation-success'; snapshot: StationDepartureSnapshot }
  | { type: 'revalidation-failure'; message?: string }

function previousSnapshot(state: DisplayInformationState): StationDepartureSnapshot | undefined {
  return 'snapshot' in state ? state.snapshot : undefined
}

function present(snapshot: StationDepartureSnapshot, kind: 'loaded' | 'empty' | 'recovered'):
  DisplayInformationState {
  if (kind === 'recovered') return { kind, snapshot }
  return snapshot.departures.length === 0 ? { kind: 'empty', snapshot } : { kind: 'loaded', snapshot }
}

export function transitionDisplayState(
  state: DisplayInformationState,
  event: DisplayInformationEvent,
): DisplayInformationState {
  switch (event.type) {
    case 'success':
      if (state.kind === 'disconnected' || state.kind === 'stale' || state.kind === 'revalidating') {
        return { kind: 'stale', snapshot: event.snapshot }
      }
      return present(event.snapshot, 'loaded')
    case 'failure':
      return previousSnapshot(state)
        ? { kind: 'stale', snapshot: previousSnapshot(state)!, message: event.message }
        : { kind: 'error', message: event.message ?? 'Departure information is unavailable.' }
    case 'offline':
      return previousSnapshot(state)
        ? { kind: 'stale', snapshot: previousSnapshot(state)! }
        : { kind: 'disconnected' }
    case 'revalidate':
      return { kind: 'revalidating', snapshot: previousSnapshot(state) }
    case 'revalidation-success':
      return present(event.snapshot, 'recovered')
    case 'revalidation-failure':
      return previousSnapshot(state)
        ? { kind: 'stale', snapshot: previousSnapshot(state)!, message: event.message }
        : { kind: 'unavailable' }
  }
}
