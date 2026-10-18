import { describe, expect, it } from 'vitest'
import { stationDisplayFixtures } from '../api/fixtures/station-display.fixture'
import { transitionDisplayState, type DisplayInformationState } from './display-state'

const snapshot = stationDisplayFixtures[0]

describe('display information state transitions', () => {
  it('shows unavailable when disconnected before any data succeeds', () => {
    expect(transitionDisplayState({ kind: 'loading' }, { type: 'revalidation-failure' })).toEqual({ kind: 'unavailable' })
    expect(transitionDisplayState({ kind: 'loading' }, { type: 'offline' })).toEqual({ kind: 'disconnected' })
  })

  it('does not mark a late initial response current if the display disconnected first', () => {
    const disconnected = transitionDisplayState({ kind: 'loading' }, { type: 'offline' })
    expect(transitionDisplayState(disconnected, { type: 'success', snapshot })).toEqual({ kind: 'stale', snapshot })
  })

  it('retains the last successful snapshot as stale after connectivity loss or failure', () => {
    const loaded: DisplayInformationState = { kind: 'loaded', snapshot }
    expect(transitionDisplayState(loaded, { type: 'offline' })).toEqual({ kind: 'stale', snapshot })
    expect(transitionDisplayState(loaded, { type: 'failure', message: 'offline' })).toEqual({ kind: 'stale', snapshot, message: 'offline' })
  })

  it('keeps stale data during revalidation and marks recovery only on success', () => {
    const stale: DisplayInformationState = { kind: 'stale', snapshot }
    expect(transitionDisplayState(stale, { type: 'revalidate' })).toEqual({ kind: 'revalidating', snapshot })
    expect(transitionDisplayState({ kind: 'revalidating', snapshot }, { type: 'revalidation-failure' })).toEqual({ kind: 'stale', snapshot })
    expect(transitionDisplayState({ kind: 'revalidating', snapshot }, { type: 'revalidation-success', snapshot })).toEqual({ kind: 'recovered', snapshot })
  })

  it('represents a successful empty snapshot as empty', () => {
    const empty = stationDisplayFixtures[1]
    expect(transitionDisplayState({ kind: 'loading' }, { type: 'success', snapshot: empty })).toEqual({ kind: 'empty', snapshot: empty })
  })
})
