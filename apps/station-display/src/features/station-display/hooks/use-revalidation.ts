import { useEffect, useRef } from 'react'
import type { StationDepartureSource } from '../api/station-departure-source'
import type { StationDepartureSnapshot } from '../model/display-model'
import type { DisplayInformationEvent } from '../model/display-state'

type Dispatch = (event: DisplayInformationEvent) => void

/** Reconnect only starts a source check. Stale data clears only after that check succeeds. */
export function useRevalidation(
  source: StationDepartureSource,
  stationId: string,
  dispatch: Dispatch,
): void {
  const running = useRef(false)

  useEffect(() => {
    let active = true
    const handleOffline = () => dispatch({ type: 'offline' })
    const handleOnline = () => {
      if (running.current) return
      running.current = true
      dispatch({ type: 'revalidate' })
      void source.revalidate(stationId).then(
        (snapshot: StationDepartureSnapshot) => {
          if (active && snapshot.station.stationId === stationId) dispatch({ type: 'revalidation-success', snapshot })
          else if (active) dispatch({ type: 'revalidation-failure', message: 'Revalidated information did not match this station.' })
        },
        () => {
          if (active) dispatch({ type: 'revalidation-failure', message: 'Departure information could not be revalidated.' })
        },
      ).finally(() => {
        running.current = false
      })
    }
    window.addEventListener('offline', handleOffline)
    window.addEventListener('online', handleOnline)
    return () => {
      active = false
      window.removeEventListener('offline', handleOffline)
      window.removeEventListener('online', handleOnline)
    }
  }, [dispatch, source, stationId])
}
