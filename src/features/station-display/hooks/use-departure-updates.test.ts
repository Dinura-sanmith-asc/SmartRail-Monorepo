import { act, renderHook, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { createFixtureDepartureSource } from '../api/fixtures/fixture-departure-source'
import { DEMO_STATION_IDS, stationDisplayFixtures } from '../api/fixtures/station-display.fixture'
import type { StationDepartureSnapshot } from '../model/display-model'
import type { StationDepartureSource } from '../api/station-departure-source'
import { useDepartureUpdates } from './use-departure-updates'

function controlledSource(initialSnapshot: Promise<StationDepartureSnapshot> | StationDepartureSnapshot) {
  let deliver: ((snapshot: StationDepartureSnapshot) => void) | undefined
  const source: StationDepartureSource = {
    getInitialSnapshot: vi.fn(() => Promise.resolve(initialSnapshot)),
    revalidate: vi.fn(),
    subscribe: vi.fn((listener) => {
      deliver = listener
      return () => {
        deliver = undefined
      }
    }),
  }

  return {
    source,
    deliver(snapshot: StationDepartureSnapshot) {
      deliver?.(snapshot)
    },
  }
}

describe('useDepartureUpdates', () => {
  it('loads the initial snapshot from the source without passenger action', async () => {
    const fixture = stationDisplayFixtures[0]
    const { source } = controlledSource(fixture)
    const { result } = renderHook(() => useDepartureUpdates(source, DEMO_STATION_IDS.central))

    await waitFor(() => expect(result.current).toMatchObject({ kind: 'loaded', snapshot: fixture }))
    expect(source.getInitialSnapshot).toHaveBeenCalledWith(DEMO_STATION_IDS.central)
    expect(source.revalidate).not.toHaveBeenCalled()
  })

  it('updates the displayed snapshot when the source delivers a newer fixture snapshot', async () => {
    const fixture = stationDisplayFixtures[0]
    const controlled = controlledSource(fixture)
    const { result } = renderHook(() => useDepartureUpdates(controlled.source, DEMO_STATION_IDS.central))
    await waitFor(() => expect(result.current.kind).toBe('loaded'))

    const updatedSnapshot: StationDepartureSnapshot = {
      ...fixture,
      departures: [fixture.departures[1], fixture.departures[0]],
    }
    act(() => controlled.deliver(updatedSnapshot))

    expect(result.current).toEqual({ kind: 'loaded', snapshot: updatedSnapshot })
  })

  it('ignores source snapshots for any station other than the configured station', async () => {
    const fixture = stationDisplayFixtures[0]
    const otherStation = stationDisplayFixtures[2]
    const controlled = controlledSource(fixture)
    const { result } = renderHook(() => useDepartureUpdates(controlled.source, DEMO_STATION_IDS.central))
    await waitFor(() => expect(result.current).toMatchObject({ kind: 'loaded', snapshot: fixture }))

    act(() => controlled.deliver(otherStation))

    expect(result.current).toEqual({ kind: 'loaded', snapshot: fixture })
  })

  it('presents source failure as unavailable without fabricating a snapshot', async () => {
    const controlled = controlledSource(Promise.reject(new Error('fixture source failure')))
    const { result } = renderHook(() => useDepartureUpdates(controlled.source, DEMO_STATION_IDS.central))

    await waitFor(() => expect(result.current).toEqual({ kind: 'error', message: 'Departure information is unavailable.' }))
    expect(controlled.source.revalidate).not.toHaveBeenCalled()
  })

  it('accepts fixture source-delivered changes without exposing a refresh action', async () => {
    const fixture = stationDisplayFixtures[0]
    const source = createFixtureDepartureSource([fixture])
    const { result } = renderHook(() => useDepartureUpdates(source, DEMO_STATION_IDS.central))
    await waitFor(() => expect(result.current.kind).toBe('loaded'))

    const updated = { ...fixture, departures: [fixture.departures[1], fixture.departures[0]] }
    act(() => source.publish(updated))

    expect(result.current).toEqual({ kind: 'loaded', snapshot: updated })
  })

  it('retains stale information until an online-triggered authoritative revalidation succeeds', async () => {
    const fixture = stationDisplayFixtures[0]
    let refreshHandlers: Parameters<StationDepartureSource['subscribe']>[1]
    let resolveRevalidation: ((snapshot: StationDepartureSnapshot) => void) | undefined
    const source: StationDepartureSource = {
      getInitialSnapshot: vi.fn().mockResolvedValue(fixture),
      revalidate: vi.fn(() => new Promise<StationDepartureSnapshot>((resolve) => { resolveRevalidation = resolve })),
      subscribe: vi.fn((_listener, handlers) => {
        refreshHandlers = handlers
        return () => undefined
      }),
    }
    const { result } = renderHook(() => useDepartureUpdates(source, DEMO_STATION_IDS.central))
    await waitFor(() => expect(result.current.kind).toBe('loaded'))

    act(() => refreshHandlers?.onRefreshFailure?.('refresh failed'))
    expect(result.current).toMatchObject({ kind: 'stale', snapshot: fixture })
    act(() => window.dispatchEvent(new Event('online')))
    expect(result.current).toMatchObject({ kind: 'revalidating', snapshot: fixture })
    expect(source.revalidate).toHaveBeenCalledTimes(1)

    const updated = { ...fixture, departures: [fixture.departures[1]] }
    act(() => resolveRevalidation?.(updated))
    await waitFor(() => expect(result.current).toEqual({ kind: 'recovered', snapshot: updated }))
  })
})
