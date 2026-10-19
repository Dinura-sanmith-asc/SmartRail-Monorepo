import type { StationDepartureSnapshot } from '../../model/display-model'
import type { StationDepartureSource } from '../station-departure-source'

interface BrowserTestControl {
  sourceAvailable: boolean
  platformChangeNotice?: boolean
  holdNextRevalidation: boolean
  revalidationCalls: number
  pendingRevalidation?: {
    resolve(snapshot: StationDepartureSnapshot): void
    reject(error: Error): void
    snapshot: StationDepartureSnapshot
  }
}

type TestWindow = Window & { __stationDisplayE2E?: BrowserTestControl }

/** Development-only browser control for deterministic offline/revalidation acceptance scenarios. */
export function createDevControlledDepartureSource(source: StationDepartureSource): StationDepartureSource {
  const control = (window as TestWindow).__stationDisplayE2E
  if (!control) return source

  let lastSnapshot: StationDepartureSnapshot | undefined
  const prepare = (snapshot: StationDepartureSnapshot) => {
    if (!control.platformChangeNotice || snapshot.departures.length === 0) return snapshot
    return {
      ...snapshot,
      departures: snapshot.departures.map((departure, index) => index === 0
        ? {
            ...departure,
            operational: {
              ...departure.operational,
              platform: { kind: 'known' as const, value: 'DEMO platform change notice: platform 2' },
            },
          }
        : departure),
    }
  }
  const handleResolution = (event: Event) => {
    const pending = control.pendingRevalidation
    if (!pending) return
    const detail = (event as CustomEvent<{ ok: boolean; serviceLabel?: string }>).detail
    delete control.pendingRevalidation
    if (!detail.ok) {
      pending.reject(new Error('Controlled fixture revalidation failed.'))
      return
    }
    const snapshot = prepare(detail.serviceLabel
      ? {
          ...pending.snapshot,
          departures: pending.snapshot.departures.map((departure, index) =>
            index === 0 ? { ...departure, serviceLabel: detail.serviceLabel! } : departure,
          ),
        }
      : pending.snapshot)
    lastSnapshot = snapshot
    pending.resolve(snapshot)
  }
  let listening = false

  return {
    async getInitialSnapshot(stationId) {
      if (!control.sourceAvailable) throw new Error('Controlled fixture source is unavailable.')
      const snapshot = prepare(await source.getInitialSnapshot(stationId))
      lastSnapshot = snapshot
      return snapshot
    },
    async revalidate(stationId) {
      control.revalidationCalls += 1
      if (!control.sourceAvailable) throw new Error('Controlled fixture source is unavailable.')
      const snapshot = lastSnapshot ?? await source.getInitialSnapshot(stationId)
      if (!control.holdNextRevalidation) return snapshot

      control.holdNextRevalidation = false
      return new Promise<StationDepartureSnapshot>((resolve, reject) => {
        control.pendingRevalidation = { resolve, reject, snapshot }
      })
    },
    subscribe(listener) {
      if (!listening) {
        window.addEventListener('station-display:e2e-resolve-revalidation', handleResolution)
        listening = true
      }
      const unsubscribe = source.subscribe((snapshot) => {
        if (snapshot.station.stationId === lastSnapshot?.station.stationId) lastSnapshot = snapshot
        listener(snapshot)
      })
      return () => {
        unsubscribe()
        window.removeEventListener('station-display:e2e-resolve-revalidation', handleResolution)
        listening = false
      }
    },
  }
}
