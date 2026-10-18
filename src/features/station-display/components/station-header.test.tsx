import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { StationHeader } from './station-header'

describe('StationHeader', () => {
  it('renders the authoritative published station name', () => {
    render(<StationHeader state={{ kind: 'ready', station: { stationId: 'test-id', name: 'Published Test Station' } }} />)

    expect(screen.getByRole('heading', { name: 'Published Test Station', level: 1 })).toBeInTheDocument()
    expect(screen.queryByRole('button')).not.toBeInTheDocument()
  })

  it('renders a loading state while station information is being checked', () => {
    render(<StationHeader state={{ kind: 'loading' }} />)

    expect(screen.getByRole('status')).toHaveTextContent('Loading station information')
  })

  it('renders a clear unavailable state without a station selector', () => {
    render(<StationHeader state={{ kind: 'unavailable', message: 'Station information is unavailable.' }} />)

    expect(screen.getByRole('alert')).toHaveTextContent('Station information is unavailable.')
    expect(screen.queryByRole('combobox')).not.toBeInTheDocument()
    expect(screen.queryByRole('button')).not.toBeInTheDocument()
  })
})
