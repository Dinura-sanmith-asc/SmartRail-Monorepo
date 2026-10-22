import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import type { OperationalInformation } from '../model/display-model'
import { OperationalChange } from './operational-change'

describe('OperationalChange', () => {
  it('presents a supplied platform change and delay as labeled text', () => {
    const operational: OperationalInformation = {
      status: { kind: 'known', value: 'DEMO delayed' },
      platform: { kind: 'known', value: 'DEMO platform change example' },
      delay: { kind: 'known', value: 'DEMO delay example' },
      cancellation: { kind: 'known', value: false },
    }

    render(<OperationalChange operational={operational} />)

    expect(screen.getByText('Platform')).toBeInTheDocument()
    expect(screen.getByText('DEMO platform change example')).toBeInTheDocument()
    expect(screen.getByText('Delay')).toBeInTheDocument()
    expect(screen.getByText('DEMO delay example')).toBeInTheDocument()
    expect(screen.getByText('Status')).toBeInTheDocument()
    expect(screen.getByText('DEMO delayed')).toBeInTheDocument()
    expect(screen.getByText('No cancellation reported')).toBeInTheDocument()
  })

  it('prominently identifies a supplied cancellation with a text label', () => {
    const operational: OperationalInformation = {
      status: { kind: 'unknown' },
      platform: { kind: 'unavailable' },
      delay: { kind: 'unknown' },
      cancellation: { kind: 'known', value: true },
    }

    render(<OperationalChange operational={operational} />)

    expect(screen.getByRole('status', { name: 'Cancellation status: Cancelled' })).toHaveTextContent('Cancelled')
  })

  it('shows a supplied update time only when its meaning is explicit', () => {
    const operational: OperationalInformation = {
      status: { kind: 'unknown' },
      platform: { kind: 'unknown' },
      delay: { kind: 'unknown' },
      cancellation: { kind: 'unknown' },
      updateTime: { displayValue: 'DEMO fixture time example', meaning: 'fixture-defined' },
    }

    const { rerender } = render(<OperationalChange operational={operational} />)
    expect(screen.getByText('Fixture update time')).toBeInTheDocument()
    expect(screen.getByText('DEMO fixture time example')).toBeInTheDocument()

    rerender(
      <OperationalChange
        operational={{ ...operational, updateTime: { displayValue: 'Unverified time', meaning: 'unverified' } }}
      />,
    )
    expect(screen.queryByText(/Unverified time/)).not.toBeInTheDocument()
  })

  it('does not infer values from unknown or unavailable information', () => {
    const operational: OperationalInformation = {
      status: { kind: 'unknown' },
      platform: { kind: 'unknown' },
      delay: { kind: 'unavailable' },
      cancellation: { kind: 'unknown' },
    }

    render(<OperationalChange operational={operational} />)

    expect(screen.getByText('Status')).toBeInTheDocument()
    expect(screen.getByText('Platform')).toBeInTheDocument()
    expect(screen.getByText('Delay')).toBeInTheDocument()
    expect(screen.getByText('Cancellation')).toBeInTheDocument()
    expect(screen.getAllByText('Unknown')).toHaveLength(3)
    expect(screen.getByText('Unavailable')).toBeInTheDocument()
    expect(screen.queryByText(/minutes/)).not.toBeInTheDocument()
  })
})
