import { render, screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { stationDisplayFixtures } from '../api/fixtures/station-display.fixture'
import type { StationDepartureSnapshot } from '../model/display-model'
import { DepartureBoard } from './departure-board'

const central = stationDisplayFixtures[0]
const quiet = stationDisplayFixtures[1]

describe('DepartureBoard', () => {
  it('preserves source order and renders only the configured station snapshot', () => {
    render(<DepartureBoard configuredStationId={central.station.stationId} snapshot={central} />)

    const rows = screen.getAllByRole('listitem')
    expect(rows).toHaveLength(central.departures.length)
    expect(within(rows[0]).getByText('DEMO TEST Service A')).toBeInTheDocument()
    expect(within(rows[1]).getByText('DEMO TEST Service B')).toBeInTheDocument()
    expect(screen.queryByText('DEMO TEST Service C')).not.toBeInTheDocument()
  })

  it('keeps each operational change attached to its source departure row', () => {
    render(<DepartureBoard configuredStationId={central.station.stationId} snapshot={central} />)

    const rows = screen.getAllByRole('listitem')
    expect(within(rows[0]).getByText('Status: DEMO delayed')).toBeInTheDocument()
    expect(within(rows[0]).getByText('Platform: DEMO platform 2')).toBeInTheDocument()
    expect(within(rows[1]).getByRole('status')).toHaveTextContent('Cancelled')
    expect(within(rows[1]).queryByText('Platform: DEMO platform 2')).not.toBeInTheDocument()
  })

  it('refuses a snapshot associated with a different station instead of falling back', () => {
    render(<DepartureBoard configuredStationId={central.station.stationId} snapshot={stationDisplayFixtures[2]} />)

    expect(screen.getByRole('alert')).toHaveTextContent('Departure information is unavailable for this station.')
    expect(screen.queryByText('DEMO TEST Service C')).not.toBeInTheDocument()
  })

  it('shows an explicit empty state and keeps the non-live label visible', () => {
    render(<DepartureBoard configuredStationId={quiet.station.stationId} snapshot={quiet} />)

    expect(screen.getByText('No fixture departures are available for this station.')).toBeInTheDocument()
    expect(screen.getByText('DEMO DATA — NOT LIVE')).toBeInTheDocument()
  })

  it('does not turn planned service search data into departures', () => {
    const emptySnapshot: StationDepartureSnapshot = {
      ...quiet,
      station: { ...quiet.station, stationId: 'demo-empty-search' },
      departures: [],
    }

    render(<DepartureBoard configuredStationId="demo-empty-search" snapshot={emptySnapshot} />)

    expect(screen.getByText('No fixture departures are available for this station.')).toBeInTheDocument()
    expect(screen.queryByText(/service search/i)).not.toBeInTheDocument()
  })

  it('keeps the DEMO label present after the rendered source snapshot changes', () => {
    const { rerender } = render(<DepartureBoard configuredStationId={central.station.stationId} snapshot={central} />)
    const nextSnapshot = { ...central, departures: [central.departures[1], central.departures[0]] }

    rerender(<DepartureBoard configuredStationId={central.station.stationId} snapshot={nextSnapshot} />)

    expect(screen.getByText('DEMO DATA — NOT LIVE')).toBeInTheDocument()
    const rows = screen.getAllByRole('listitem')
    expect(within(rows[0]).getByText('DEMO TEST Service B')).toBeInTheDocument()
  })
})
