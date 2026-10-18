import { useEffect, useState } from 'react'
import { readStationDisplayConfig, type ConfigErrorReason } from './app/config'
import { findPublishedStation } from './features/station-display/api/station-reference-client'
import { createFixtureDepartureSource } from './features/station-display/api/fixtures/fixture-departure-source'
import { stationDisplayFixtures } from './features/station-display/api/fixtures/station-display.fixture'
import type { StationDepartureSnapshot } from './features/station-display/model/display-model'
import type { StationDepartureSource } from './features/station-display/api/station-departure-source'
import { StationHeader, type StationHeaderState } from './features/station-display/components/station-header'
import { DepartureBoard } from './features/station-display/components/departure-board'
import { createHttpClient } from './shared/api/http-client'

const stationDisplayConfig = readStationDisplayConfig()
const fixtureDepartureSource = createFixtureDepartureSource(stationDisplayFixtures)

function configurationMessage(reason: ConfigErrorReason): string {
  switch (reason) {
    case 'missing-station-id':
      return 'Display station configuration is missing.'
    case 'invalid-station-id':
      return 'Display station configuration is invalid.'
    case 'missing-api-base-url':
    case 'invalid-api-base-url':
      return 'Station information is unavailable because API configuration is invalid.'
  }
}

function FixtureDepartures({ stationId, source }: { stationId: string; source: StationDepartureSource }) {
  const [snapshot, setSnapshot] = useState<StationDepartureSnapshot | null>(null)
  const [unavailable, setUnavailable] = useState(false)

  useEffect(() => {
    let active = true
    const unsubscribe = source.subscribe((nextSnapshot) => {
      if (active && nextSnapshot.station.stationId === stationId) setSnapshot(nextSnapshot)
    })

    void source
      .getInitialSnapshot(stationId)
      .then((initialSnapshot) => {
        if (active) setSnapshot(initialSnapshot)
      })
      .catch(() => {
        if (active) setUnavailable(true)
      })

    return () => {
      active = false
      unsubscribe()
    }
  }, [source, stationId])

  if (snapshot) return <DepartureBoard configuredStationId={stationId} snapshot={snapshot} />
  if (unavailable) {
    return (
      <p className="m-8 text-lg text-amber-200" role="alert">
        DEMO departure fixture is unavailable for this configured station.
      </p>
    )
  }
  return <p className="m-8 text-lg text-slate-200" role="status">Loading DEMO departure fixture</p>
}

interface AppProps {
  departureSource?: StationDepartureSource
}

function App({ departureSource = fixtureDepartureSource }: AppProps) {
  const [state, setState] = useState<StationHeaderState>(() =>
    stationDisplayConfig.kind === 'valid'
      ? { kind: 'loading' }
      : { kind: 'unavailable', message: configurationMessage(stationDisplayConfig.reason) },
  )

  useEffect(() => {
    if (stationDisplayConfig.kind === 'invalid') return

    let active = true
    const client = createHttpClient({ baseUrl: stationDisplayConfig.config.apiBaseUrl })
    void findPublishedStation(stationDisplayConfig.config.stationId, client).then((lookup) => {
      if (!active) return
      if (lookup.kind === 'found') {
        setState({ kind: 'ready', station: lookup.station })
      } else if (lookup.kind === 'unpublished') {
        setState({ kind: 'unavailable', message: 'The configured station is not published.' })
      } else {
        setState({ kind: 'unavailable', message: 'Station information is unavailable.' })
      }
    })

    return () => {
      active = false
    }
  }, [])

  return (
    <main className="flex min-h-screen flex-col bg-slate-950">
      <StationHeader state={state} />
      {state.kind === 'ready' ? <FixtureDepartures stationId={state.station.stationId} source={departureSource} /> : null}
    </main>
  )
}

export default App
