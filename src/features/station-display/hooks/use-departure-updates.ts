import { useEffect, useReducer } from 'react'
import type { StationDepartureSource } from '../api/station-departure-source'
import { transitionDisplayState, type DisplayInformationState } from '../model/display-state'
import { useRevalidation } from './use-revalidation'

interface ScopedState {
  stationId: string
  source: StationDepartureSource
  state: DisplayInformationState
}

/** One source subscription owns snapshots; reconnect checks reuse the same source without a second subscription. */
export function useDepartureUpdates(source: StationDepartureSource, stationId: string): DisplayInformationState {
  const [entry, dispatch] = useReducer(
    (current: ScopedState, event: Parameters<typeof transitionDisplayState>[1]) => ({
      ...current,
      state: transitionDisplayState(current.state, event),
    }),
    { stationId, source, state: { kind: 'loading' } as DisplayInformationState },
  )

  useRevalidation(source, stationId, dispatch)

  useEffect(() => {
    let active = true
    let receivedSourceUpdate = false
    const unsubscribe = source.subscribe((snapshot) => {
      if (!active || snapshot.station.stationId !== stationId) return
      receivedSourceUpdate = true
      dispatch({ type: 'success', snapshot })
    }, {
      onRevalidated: (snapshot) => {
        if (active && snapshot.station.stationId === stationId) dispatch({ type: 'revalidation-success', snapshot })
      },
      onRefreshFailure: (message) => {
        if (active) dispatch({ type: 'failure', message })
      },
    })

    void source.getInitialSnapshot(stationId).then(
      (snapshot) => {
        if (!active || receivedSourceUpdate) return
        if (snapshot.station.stationId !== stationId) dispatch({ type: 'failure', message: 'Departure information is unavailable for this station.' })
        else dispatch({ type: 'success', snapshot })
      },
      () => { if (active) dispatch({ type: 'failure', message: 'Departure information is unavailable.' }) },
    )

    return () => {
      active = false
      unsubscribe()
    }
  }, [source, stationId])

  return entry.stationId === stationId && entry.source === source ? entry.state : { kind: 'loading' }
}
