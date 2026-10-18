import { useEffect, useState } from 'react'
import { readStationDisplayConfig, type ConfigErrorReason } from './app/config'
import { findPublishedStation } from './features/station-display/api/station-reference-client'
import { createFixtureDepartureSource } from './features/station-display/api/fixtures/fixture-departure-source'
import { stationDisplayFixtures } from './features/station-display/api/fixtures/station-display.fixture'
import type { StationDepartureSource } from './features/station-display/api/station-departure-source'
import { StationHeader, type StationHeaderState } from './features/station-display/components/station-header'
import { DisplayStateView } from './features/station-display/components/display-state-view'
import { useDepartureUpdates } from './features/station-display/hooks/use-departure-updates'
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
  const state = useDepartureUpdates(source, stationId)

  return <DisplayStateView configuredStationId={stationId} state={state} />
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
