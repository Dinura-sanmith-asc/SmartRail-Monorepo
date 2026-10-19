import { expect, request, test } from '@playwright/test'

interface Departure {
  journeyInstanceId: string
  serviceId: string
  operationDate: string
  stationLocalDepartureDate: string
  revision: number
  status: string
  stationId: string
  stationName?: string
  stopOrder: number
  scheduledDepartureAt: string | null
  operationalDepartureAt?: string | null
  platform?: string | null
  source?: string
  updatedAt?: string
  ambiguity: string
}

interface JourneySnapshot {
  journeyInstanceId: string
  revision: number
  stops: Array<{
    journeyStopId: string
    stopOrder: number
    stationId: string
    stationName: string
    platform: string | null
  }>
}

const stationId = process.env.STATION_DISPLAY_LIVE_STATION_ID
const apiBaseUrl = process.env.STATION_DISPLAY_LIVE_API_BASE_URL?.replace(/\/$/, '')
const journeyId = process.env.STATION_DISPLAY_LIVE_JOURNEY_ID
const controllerToken = process.env.STATION_DISPLAY_LIVE_CONTROLLER_TOKEN
const dedicatedEnvironment = process.env.STATION_DISPLAY_LIVE_ENVIRONMENT
const mutationConfirmation = process.env.STATION_DISPLAY_LIVE_MUTATION_CONFIRMATION

function stationLocalDate(): string {
  const parts = new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Asia/Colombo', year: 'numeric', month: '2-digit', day: '2-digit',
  }).formatToParts(new Date())
  const get = (type: string) => parts.find((part) => part.type === type)?.value
  return `${get('year')}-${get('month')}-${get('day')}`
}

function sameDepartureMeaning(left: Departure, right: Departure) {
  return {
    journeyInstanceId: left.journeyInstanceId === right.journeyInstanceId,
    serviceId: left.serviceId === right.serviceId,
    operationDate: left.operationDate === right.operationDate,
    stationLocalDepartureDate: left.stationLocalDepartureDate === right.stationLocalDepartureDate,
    status: left.status === right.status,
    stationId: left.stationId === right.stationId,
    stopOrder: left.stopOrder === right.stopOrder,
    scheduledDepartureAt: left.scheduledDepartureAt === right.scheduledDepartureAt,
    operationalDepartureAt: left.operationalDepartureAt === right.operationalDepartureAt,
    source: right.source === 'ControllerManual',
    ambiguity: left.ambiguity === right.ambiguity,
  }
}

test('a published Controller platform change reaches the live board on automatic polling', async ({ page }, testInfo) => {
  test.setTimeout(120_000)
  expect(stationId, 'configured live test station').toBeTruthy()
  expect(stationId).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i)
  expect(apiBaseUrl, 'configured live API base URL').toBeTruthy()
  expect(journeyId, 'dedicated live test journey').toBeTruthy()
  expect(journeyId).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i)
  expect(controllerToken, 'Controller access token supplied only to the Node test process').toBeTruthy()
  expect(dedicatedEnvironment, 'dedicated development/test environment declaration').toMatch(/^(development|test)$/)
  expect(mutationConfirmation, 'explicit confirmation that this is a dedicated mutation-safe environment')
    .toBe('I_CONFIRM_DEDICATED_TEST_ENV')

  const api = await request.newContext({
    baseURL: apiBaseUrl,
    extraHTTPHeaders: { Authorization: `Bearer ${controllerToken}` },
  })

  try {
    const date = stationLocalDate()
    const departuresPath = `/stations/${stationId}/departures?date=${date}`
    const journeyResponse = await api.get(`/journeys/${journeyId}`)
    expect(journeyResponse.status(), 'the dedicated journey must exist in the migrated backend').toBe(200)
    const journey = await journeyResponse.json() as JourneySnapshot
    expect(journey.journeyInstanceId).toBe(journeyId)

    const initialResponse = await api.get(departuresPath)
    expect(initialResponse.status(), 'the station-departures endpoint must return the test journey').toBe(200)
    const initialDepartures = await initialResponse.json() as Departure[]
    const targetMatches = initialDepartures.filter((item) => item.journeyInstanceId === journeyId && item.stationId === stationId)
    expect(targetMatches, 'the dedicated journey must have exactly one departure at the configured station').toHaveLength(1)
    const before = targetMatches[0]
    const targetStop = journey.stops.find((stop) => stop.stationId === stationId && stop.stopOrder === before.stopOrder)
    expect(targetStop, 'the published departure must resolve to its stable journey stop identity').toBeTruthy()

    const browserSnapshotResponse = page.waitForResponse((response) =>
      response.url().includes(`/stations/${stationId}/departures?date=${date}`) && response.status() === 200,
    )
    await page.goto('/')
    const browserSnapshot = await browserSnapshotResponse
    const displayedDepartures = await browserSnapshot.json() as Departure[]
    expect(displayedDepartures).toEqual(initialDepartures)
    await expect(page.getByRole('heading', { name: targetStop!.stationName })).toBeVisible()
    await expect(page.getByText('DEMO DATA — NOT LIVE')).toHaveCount(0)
    await expect(page.locator('body')).not.toContainText(/\b(?:within|every)\s+\d+\s+seconds?\b|\bSLA\b/i)

    const rowSelector = `[data-departure-id="${journeyId}:${before.stopOrder}"]`
    const targetRow = page.locator(rowSelector)
    await expect(targetRow).toContainText(`Service ID ${before.serviceId}`)
    await expect(targetRow).toContainText(before.platform ?? 'Unknown')
    await testInfo.attach('live-departure-before.json', {
      body: JSON.stringify({ date, stationId, departure: before }, null, 2),
      contentType: 'application/json',
    })
    await testInfo.attach('live-board-before.png', { body: await page.screenshot(), contentType: 'image/png' })

    const publishedPlatform = before.platform === 'T030-A' ? 'T030-B' : 'T030-A'
    const publicationResponse = await api.patch(
      `/operations/journeys/${journeyId}/stops/${targetStop!.journeyStopId}`,
      { data: { expectedJourneyRevision: journey.revision, platform: publishedPlatform } },
    )
    expect(publicationResponse.status(), 'the backend must accept an authorized Controller change').toBe(200)
    const publishedJourney = await publicationResponse.json() as JourneySnapshot
    expect(publishedJourney.revision).toBe(journey.revision + 1)
    expect(publishedJourney.stops.find((stop) => stop.journeyStopId === targetStop!.journeyStopId)?.platform)
      .toBe(publishedPlatform)

    const refreshedSnapshotResponse = page.waitForResponse(async (response) => {
      if (!response.url().includes(`/stations/${stationId}/departures?date=${date}`) || response.status() !== 200) return false
      const rows = await response.json() as Departure[]
      return rows.some((item) => item.journeyInstanceId === journeyId && item.stopOrder === before.stopOrder &&
        item.revision === publishedJourney.revision && item.platform === publishedPlatform)
    }, { timeout: 75_000 })
    await refreshedSnapshotResponse

    const finalReadResponse = await api.get(departuresPath)
    expect(finalReadResponse.status()).toBe(200)
    const finalDepartures = await finalReadResponse.json() as Departure[]
    expect(finalDepartures).toHaveLength(initialDepartures.length)
    for (let index = 0; index < initialDepartures.length; index += 1) {
      const initialItem = initialDepartures[index]
      const finalItem = finalDepartures[index]
      if (initialItem.journeyInstanceId === journeyId && initialItem.stopOrder === before.stopOrder) {
        expect(finalItem.platform).toBe(publishedPlatform)
        expect(sameDepartureMeaning(initialItem, finalItem)).toEqual({
          journeyInstanceId: true, serviceId: true, operationDate: true, stationLocalDepartureDate: true,
          status: true, stationId: true, stopOrder: true, scheduledDepartureAt: true,
          operationalDepartureAt: true, source: true, ambiguity: true,
        })
      } else {
        expect(finalItem).toEqual(initialItem)
      }
    }

    await expect(targetRow).toContainText(publishedPlatform)
    await expect(page.getByText('DEMO DATA — NOT LIVE')).toHaveCount(0)
    await expect(page.locator('body')).not.toContainText(/\b(?:within|every)\s+\d+\s+seconds?\b|\bSLA\b/i)
    await testInfo.attach('live-departure-after.json', {
      body: JSON.stringify({ date, stationId, departure: finalDepartures.find((item) =>
        item.journeyInstanceId === journeyId && item.stopOrder === before.stopOrder) }, null, 2),
      contentType: 'application/json',
    })
    await testInfo.attach('live-board-after.png', { body: await page.screenshot(), contentType: 'image/png' })
  } finally {
    await api.dispose()
  }
})
