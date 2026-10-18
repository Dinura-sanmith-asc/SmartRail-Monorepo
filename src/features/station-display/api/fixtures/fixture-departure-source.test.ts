import { describe, expect, it, vi } from 'vitest'
import { stationDisplayFixtures } from './station-display.fixture'
import { createFixtureDepartureSource } from './fixture-departure-source'

describe('fixture departure source', () => {
  it('returns an explicitly non-live initial snapshot', async () => {
    const source = createFixtureDepartureSource(stationDisplayFixtures)

    const snapshot = await source.getInitialSnapshot('demo-central')

    expect(snapshot.origin).toBe('fixture')
    expect(snapshot.station.stationId).toBe('demo-central')
    expect(snapshot.departures.length).toBeGreaterThan(0)
  })

  it('delivers controlled changes through the shared source interface', async () => {
    const source = createFixtureDepartureSource(stationDisplayFixtures)
    const listener = vi.fn()
    const unsubscribe = source.subscribe(listener)
    const nextSnapshot = await source.revalidate('demo-central')

    source.publish(nextSnapshot)

    expect(listener).toHaveBeenCalledWith(nextSnapshot)
    expect(listener.mock.calls[0][0].origin).toBe('fixture')
    unsubscribe()
    source.publish(nextSnapshot)
    expect(listener).toHaveBeenCalledTimes(1)
  })

  it('returns an empty departure set for a fixture station with no departures', async () => {
    const source = createFixtureDepartureSource(stationDisplayFixtures)

    await expect(source.getInitialSnapshot('demo-quiet')).resolves.toMatchObject({
      origin: 'fixture',
      departures: [],
    })
  })
})
