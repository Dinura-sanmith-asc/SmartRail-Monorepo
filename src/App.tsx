import { useEffect, useState } from 'react'
import { readStationDisplayConfig, type ConfigErrorReason } from './app/config'
import { findPublishedStation } from './features/station-display/api/station-reference-client'
import { createFixtureDepartureSource } from './features/station-display/api/fixtures/fixture-departure-source'
import { stationDisplayFixtures } from './features/station-display/api/fixtures/station-display.fixture'
import { createStationDepartureAdapter } from './features/station-display/api/station-departure-adapter'
import type { StationDepartureSource } from './features/station-display/api/station-departure-source'
import { StationHeader, type StationHeaderState } from './features/station-display/components/station-header'
import { DisplayStateView } from './features/station-display/components/display-state-view'
import { useDepartureUpdates } from './features/station-display/hooks/use-departure-updates'
import { createHttpClient } from './shared/api/http-client'
import './features/station-display/station-display.css'
import { DisplayLanguageProvider, useDisplayLanguage } from './app/language'

const stationDisplayConfig = readStationDisplayConfig()
const fixtureDepartureSource = createFixtureDepartureSource(stationDisplayFixtures)
const liveDepartureSource = stationDisplayConfig.kind === 'valid'
  ? createStationDepartureAdapter(
      createHttpClient({ baseUrl: stationDisplayConfig.config.apiBaseUrl }),
      undefined,
      stationDisplayConfig.config.refreshIntervalMs,
    )
  : fixtureDepartureSource
const defaultDepartureSource = import.meta.env.PROD || import.meta.env.VITE_STATION_DISPLAY_SOURCE === 'live'
  ? liveDepartureSource
  : fixtureDepartureSource

function configurationMessage(reason: ConfigErrorReason): string {
  switch (reason) {
    case 'missing-station-id':
      return 'Display station configuration is missing.'
    case 'invalid-station-id':
      return 'Display station configuration is invalid.'
    case 'missing-api-base-url':
    case 'invalid-api-base-url':
      return 'Station information is unavailable because API configuration is invalid.'
    case 'invalid-refresh-interval':
      return 'Station information is unavailable because refresh configuration is invalid.'
  }
}

function FixtureDepartures({ stationId, source }: { stationId: string; source: StationDepartureSource }) {
  const state = useDepartureUpdates(source, stationId)

  return <DisplayStateView configuredStationId={stationId} state={state} />
}

interface AppProps {
  departureSource?: StationDepartureSource
}

function App({ departureSource = defaultDepartureSource }: AppProps) {
  return <DisplayLanguageProvider><StationDisplay departureSource={departureSource} /></DisplayLanguageProvider>
}

function StationDisplay({ departureSource = defaultDepartureSource }: AppProps) {
  const { t } = useDisplayLanguage()
  const [activeDepartureSource, setActiveDepartureSource] = useState<StationDepartureSource | null>(
    departureSource === fixtureDepartureSource
      ? (import.meta.env.DEV ? null : fixtureDepartureSource)
      : departureSource,
  )
  const [state, setState] = useState<StationHeaderState>(() =>
    stationDisplayConfig.kind === 'valid'
      ? { kind: 'loading' }
      : { kind: 'unavailable', message: t(configurationMessage(stationDisplayConfig.reason)) },
  )

  useEffect(() => {
    if (departureSource !== fixtureDepartureSource || !import.meta.env.DEV) return
    let active = true
    void import('./features/station-display/api/fixtures/dev-controlled-departure-source').then(({ createDevControlledDepartureSource }) => {
      if (active) setActiveDepartureSource(createDevControlledDepartureSource(fixtureDepartureSource))
    })
    return () => { active = false }
  }, [departureSource])

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
    <main className="station-display">
      <StationHeader state={state} />
      {state.kind === 'ready' && activeDepartureSource ? <FixtureDepartures stationId={state.station.stationId} source={activeDepartureSource} /> : null}
    </main>
  )
}

export default App
