import type { StationContext } from '../model/display-model'

export type StationHeaderState =
  | { kind: 'loading' }
  | { kind: 'ready'; station: StationContext }
  | { kind: 'unavailable'; message: string }

interface StationHeaderProps {
  state: StationHeaderState
}

export function StationHeader({ state }: StationHeaderProps) {
  return (
    <header className="w-full border-b border-slate-700 bg-slate-900 px-8 py-6 text-white">
      {state.kind === 'loading' ? (
        <p className="text-lg text-slate-200" role="status" aria-live="polite">
          Loading station information
        </p>
      ) : state.kind === 'unavailable' ? (
        <p className="text-lg text-amber-200" role="alert">
          {state.message}
        </p>
      ) : (
        <h1 className="text-3xl font-semibold tracking-wide">{state.station.name}</h1>
      )}
    </header>
  )
}
