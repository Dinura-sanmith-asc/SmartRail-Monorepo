import { act, renderHook } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import type { StationDepartureSource } from '../api/station-departure-source'
import { stationDisplayFixtures } from '../api/fixtures/station-display.fixture'
import type { DisplayInformationState } from '../model/display-state'
import { useRevalidation } from './use-revalidation'

const snapshot = stationDisplayFixtures[0]
const stationId = snapshot.station.stationId

describe('useRevalidation', () => {
  it('does not clear stale state from online alone and only commits a successful matching revalidation', async () => {
    let resolve!: (value: typeof snapshot) => void
    const source: StationDepartureSource = {
      getInitialSnapshot: vi.fn(),
      subscribe: vi.fn(() => () => undefined),
      revalidate: vi.fn(() => new Promise<typeof snapshot>((done) => { resolve = done })),
    }
    let state: DisplayInformationState = { kind: 'stale', snapshot }
    const dispatch = vi.fn((event) => { state = transition(state, event as never) })
    const { unmount } = renderHook(() => useRevalidation(source, stationId, dispatch))

    act(() => window.dispatchEvent(new Event('online')))
    expect(source.revalidate).toHaveBeenCalledWith(stationId)
    expect(state).toEqual({ kind: 'revalidating', snapshot })
    expect(state.kind).not.toBe('loaded')

    await act(async () => { resolve(snapshot); await Promise.resolve() })
    expect(state).toEqual({ kind: 'recovered', snapshot })
    unmount()
  })

  it('retains stale state when revalidation fails and ignores overlapping online events', async () => {
    let reject!: (reason: Error) => void
    const source: StationDepartureSource = {
      getInitialSnapshot: vi.fn(),
      subscribe: vi.fn(() => () => undefined),
      revalidate: vi.fn(() => new Promise<typeof snapshot>((_, fail) => { reject = fail })),
    }
    let state: DisplayInformationState = { kind: 'stale', snapshot }
    const dispatch = vi.fn((event) => { state = transition(state, event as never) })
    const { unmount } = renderHook(() => useRevalidation(source, stationId, dispatch))
    act(() => { window.dispatchEvent(new Event('online')); window.dispatchEvent(new Event('online')) })
    expect(source.revalidate).toHaveBeenCalledTimes(1)
    await act(async () => { reject(new Error('network')); await Promise.resolve() })
    expect(state).toEqual({ kind: 'stale', snapshot, message: 'Departure information could not be revalidated.' })
    unmount()
  })
})

import { transitionDisplayState as transition } from '../model/display-state'
