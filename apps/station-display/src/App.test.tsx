import { act, render, screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const mocks = vi.hoisted(() => ({
  readConfig: vi.fn(() => ({
    kind: 'valid',
    config: { stationId: '00000000-0000-4000-8000-000000000101', apiBaseUrl: 'https://api.example.test/api' },
  })),
  findStation: vi.fn(),
}))

vi.mock('./app/config', async (importOriginal) => ({
  ...(await importOriginal<typeof import('./app/config')>()),
  readStationDisplayConfig: mocks.readConfig,
}))
vi.mock('./features/station-display/api/station-reference-client', () => ({
  findPublishedStation: mocks.findStation,
}))

import App from './App'
import { createFixtureDepartureSource } from './features/station-display/api/fixtures/fixture-departure-source'
import { stationDisplayFixtures } from './features/station-display/api/fixtures/station-display.fixture'

describe('App fixture composition', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    const fixture = stationDisplayFixtures[0]
    mocks.findStation.mockResolvedValue({
      kind: 'found',
      station: { stationId: fixture.station.stationId, name: 'Published Test Station' },
    })
  })

  it('shows validated station identity and only matching fixture departures under a persistent DEMO label', async () => {
    render(<App />)

    expect(await screen.findByRole('heading', { name: 'Published Test Station', level: 1 })).toBeInTheDocument()
    expect(await screen.findByText('DEMO DATA — NOT LIVE')).toBeInTheDocument()
    expect(screen.getByText('DEMO TEST Service A')).toBeInTheDocument()
    expect(screen.getByText('DEMO TEST Service B')).toBeInTheDocument()
    expect(screen.queryByText('DEMO TEST Service C')).not.toBeInTheDocument()
  })

  it('keeps the DEMO label visible when the fixture source delivers an update', async () => {
    const fixture = stationDisplayFixtures[0]
    const source = createFixtureDepartureSource([fixture])
    render(<App departureSource={source} />)
    expect(await screen.findByText('DEMO TEST Service A')).toBeInTheDocument()

    const updatedSnapshot = {
      ...fixture,
      departures: [fixture.departures[1], fixture.departures[0]],
    }
    act(() => source.publish(updatedSnapshot))

    const rows = screen.getAllByRole('listitem')
    expect(within(rows[0]).getByText('DEMO TEST Service B')).toBeInTheDocument()
    expect(screen.getByText('DEMO DATA — NOT LIVE')).toBeInTheDocument()
  })

  it('does not render fixture departures when station validation fails', async () => {
    mocks.findStation.mockResolvedValue({ kind: 'unpublished' })
    render(<App />)

    expect(await screen.findByRole('alert')).toHaveTextContent('The configured station is not published.')
    await waitFor(() => expect(screen.queryByText('DEMO TEST Service A')).not.toBeInTheDocument())
    expect(screen.queryByText('DEMO DATA — NOT LIVE')).not.toBeInTheDocument()
  })
})
