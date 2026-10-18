import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import type { StationDeparture } from '../model/display-model'
import { DepartureRow } from './departure-row'

const departure: StationDeparture = {
  departureId: 'demo-departure-a',
  serviceDate: '2026-10-18',
  serviceLabel: 'DEMO TEST Service A',
  scheduledTime: { displayValue: '08:15' },
  operational: {
    status: { kind: 'known', value: 'DEMO delayed' },
    platform: { kind: 'unknown' },
    delay: { kind: 'unavailable' },
    cancellation: { kind: 'known', value: false },
  },
}

describe('DepartureRow', () => {
  it('shows service identity and clearly labels the scheduled time', () => {
    render(<DepartureRow departure={departure} />)

    expect(screen.getByText('DEMO TEST Service A')).toBeInTheDocument()
    expect(screen.getByText('Scheduled')).toBeInTheDocument()
    expect(screen.getByText('08:15')).toBeInTheDocument()
  })

  it('keeps unavailable and unknown operational values explicit', () => {
    render(<DepartureRow departure={departure} />)

    expect(screen.getByText('Platform unknown')).toBeInTheDocument()
    expect(screen.getByText('Delay unavailable')).toBeInTheDocument()
  })

  it('renders status meaning as text without requiring color', () => {
    render(<DepartureRow departure={departure} />)

    expect(screen.getByText('Status: DEMO delayed')).toBeInTheDocument()
  })
})
