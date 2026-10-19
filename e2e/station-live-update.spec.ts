import {
  expect,
  request,
  test,
  type APIRequestContext,
  type TestInfo,
} from '@playwright/test'

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
  serviceId: string
  scheduleId: string
  operationDate: string
  revision: number
  source: string
  stops: Array<{
    journeyStopId: string
    stopOrder: number
    stationId: string
    stationName: string
    plannedDepartureAt: string | null
    operationalDepartureAt?: string | null
    platform: string | null
  }>
}

interface StationDirectory {
  items: Array<{ stationId: string; name: string }>
}

const stationId = process.env.STATION_DISPLAY_LIVE_STATION_ID
const apiBaseUrl = process.env.STATION_DISPLAY_LIVE_API_BASE_URL?.replace(
  /\/$/,
  '',
)
const journeyId = process.env.STATION_DISPLAY_LIVE_JOURNEY_ID
const controllerToken = process.env.STATION_DISPLAY_LIVE_CONTROLLER_TOKEN
const dedicatedEnvironment = process.env.STATION_DISPLAY_LIVE_ENVIRONMENT
const mutationConfirmation =
  process.env.STATION_DISPLAY_LIVE_MUTATION_CONFIRMATION

function stationLocalDate(): string {
  const parts = new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Asia/Colombo',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).formatToParts(new Date())
  const get = (type: string) => parts.find((part) => part.type === type)?.value
  return `${get('year')}-${get('month')}-${get('day')}`
}

function sameDepartureMeaning(left: Departure, right: Departure) {
  return {
    journeyInstanceId: left.journeyInstanceId === right.journeyInstanceId,
    serviceId: left.serviceId === right.serviceId,
    operationDate: left.operationDate === right.operationDate,
    stationLocalDepartureDate:
      left.stationLocalDepartureDate === right.stationLocalDepartureDate,
    status: left.status === right.status,
    stationId: left.stationId === right.stationId,
    stopOrder: left.stopOrder === right.stopOrder,
    scheduledDepartureAt:
      left.scheduledDepartureAt === right.scheduledDepartureAt,
    operationalDepartureAt:
      left.operationalDepartureAt === right.operationalDepartureAt,
    source: right.source === 'ControllerManual',
    ambiguity: left.ambiguity === right.ambiguity,
  }
}

async function getJourney(api: APIRequestContext): Promise<JourneySnapshot> {
  const response = await api.get(`/journeys/${journeyId}`)
  expect(
    response.status(),
    'the dedicated journey must exist in the migrated backend',
  ).toBe(200)
  return (await response.json()) as JourneySnapshot
}

async function getTargetDeparture(
  api: APIRequestContext,
  date: string,
): Promise<Departure> {
  const response = await api.get(
    `/stations/${stationId}/departures?date=${date}`,
  )
  expect(
    response.status(),
    'the station-departures endpoint must return the requested date',
  ).toBe(200)
  const departures = (await response.json()) as Departure[]
  const matches = departures.filter(
    (item) =>
      item.journeyInstanceId === journeyId &&
      item.stationId === stationId &&
      item.stationLocalDepartureDate === date,
  )
  expect(
    matches,
    'the dedicated journey must have exactly one departure at the configured station for today',
  ).toHaveLength(1)
  return matches[0]
}

async function recordRestorationFailure(testInfo: TestInfo, message: string) {
  await testInfo.attach('t030-manual-recovery.txt', {
    body: message,
    contentType: 'text/plain',
  })
  throw new Error(message)
}

test('a published Controller platform change reaches the live board and is restored', async ({
  page,
}, testInfo) => {
  test.setTimeout(120_000)
  expect(stationId, 'configured live test station').toBeTruthy()
  expect(stationId).toMatch(
    /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
  )
  expect(apiBaseUrl, 'configured live API base URL').toBeTruthy()
  expect(apiBaseUrl).toMatch(/^https?:\/\//i)
  expect(journeyId, 'dedicated live test journey').toBeTruthy()
  expect(journeyId).toMatch(
    /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
  )
  expect(
    controllerToken,
    'Controller access token supplied only to the Node test process',
  ).toBeTruthy()
  expect(
    dedicatedEnvironment,
    'dedicated development/test environment declaration',
  ).toMatch(/^(development|test)$/)
  expect(
    mutationConfirmation,
    'explicit confirmation that this is a dedicated mutation-safe environment',
  ).toBe('I_CONFIRM_DEDICATED_TEST_ENV')

  const api = await request.newContext({
    baseURL: apiBaseUrl,
    extraHTTPHeaders: { Authorization: `Bearer ${controllerToken}` },
  })

  let targetStopId: string | undefined
  let originalPlatform: string | null | undefined
  let requestedPlatform: string | undefined
  let departureDate: string | undefined
  let mutationAttempted = false
  let testFailure: unknown
  let cleanupFailure: string | undefined

  try {
    const date = stationLocalDate()
    departureDate = date
    const stationResponse = await api.get('/stations')
    expect(
      stationResponse.status(),
      'published stations must be available',
    ).toBe(200)
    const stations = (await stationResponse.json()) as StationDirectory
    const configuredStation = stations.items.find(
      (station) => station.stationId === stationId,
    )
    expect(
      configuredStation?.name,
      'the configured station must be the published Colombo Fort station',
    ).toBe('Colombo Fort')

    const journey = await getJourney(api)
    expect(journey.journeyInstanceId).toBe(journeyId)
    expect(journey.serviceId).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
    )
    expect(journey.scheduleId).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
    )

    const before = await getTargetDeparture(api, date)
    expect(
      before.revision,
      'the station departure must represent the latest journey revision',
    ).toBe(journey.revision)
    const targetStop = journey.stops.find(
      (stop) =>
        stop.stationId === stationId && stop.stopOrder === before.stopOrder,
    )
    expect(
      targetStop,
      'the published departure must resolve to its stable Colombo Fort journey stop',
    ).toBeTruthy()
    targetStopId = targetStop!.journeyStopId
    expect(
      before.platform,
      'departure and journey detail must agree on the original platform',
    ).toBe(targetStop!.platform)
    expect(
      ['T030-A', 'T030-B'],
      'the configured journey must carry the dedicated T030 test-platform marker',
    ).toContain(before.platform)
    expect(before.stationLocalDepartureDate).toBe(date)

    // The backend departure contract is station-date scoped. Confirm the
    // effective source time itself belongs to today's Colombo calendar date.
    const effectiveDeparture =
      before.scheduledDepartureAt ?? before.operationalDepartureAt
    expect(
      effectiveDeparture,
      'the eligible departure must have a source-provided time',
    ).toBeTruthy()
    const effectiveLocalDate = new Intl.DateTimeFormat('en-CA', {
      timeZone: 'Asia/Colombo',
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
    }).format(new Date(effectiveDeparture!))
    expect(
      effectiveLocalDate,
      'the source-provided departure must fall on the requested Colombo date',
    ).toBe(date)

    originalPlatform = targetStop!.platform
    requestedPlatform = originalPlatform === 'T030-A' ? 'T030-B' : 'T030-A'

    const browserSnapshotResponse = page.waitForResponse(
      (response) =>
        response
          .url()
          .includes(`/stations/${stationId}/departures?date=${date}`) &&
        response.status() === 200,
    )
    await page.goto('/')
    const browserSnapshot = await browserSnapshotResponse
    const displayedDepartures = (await browserSnapshot.json()) as Departure[]
    const initialDeparturesResponse = await api.get(
      `/stations/${stationId}/departures?date=${date}`,
    )
    expect(initialDeparturesResponse.status()).toBe(200)
    const initialDepartures =
      (await initialDeparturesResponse.json()) as Departure[]
    expect(displayedDepartures).toEqual(initialDepartures)
    await expect(
      page.getByRole('heading', { name: 'Colombo Fort' }),
    ).toBeVisible()
    await expect(page.getByText('DEMO DATA — NOT LIVE')).toHaveCount(0)
    await expect(page.locator('body')).not.toContainText(
      /\b(?:within|every)\s+\d+\s+seconds?\b|\bSLA\b/i,
    )

    const rowSelector = `[data-departure-id="${journeyId}:${before.stopOrder}"]`
    const targetRow = page.locator(rowSelector)
    await expect(targetRow).toContainText(`Service ID ${before.serviceId}`)
    await expect(targetRow).toContainText(originalPlatform ?? 'Unknown')
    await testInfo.attach('live-departure-before.json', {
      body: JSON.stringify(
        { date, stationId, journeyId, stopId: targetStopId, departure: before },
        null,
        2,
      ),
      contentType: 'application/json',
    })
    await testInfo.attach('live-board-before.png', {
      body: await page.screenshot(),
      contentType: 'image/png',
    })

    const refreshedSnapshotResponse = page.waitForResponse(
      async (response) => {
        if (
          !response
            .url()
            .includes(`/stations/${stationId}/departures?date=${date}`) ||
          response.status() !== 200
        )
          return false
        const rows = (await response.json()) as Departure[]
        return rows.some(
          (item) =>
            item.journeyInstanceId === journeyId &&
            item.stopOrder === before.stopOrder &&
            item.revision === journey.revision + 1 &&
            item.platform === requestedPlatform,
        )
      },
      { timeout: 75_000 },
    )

    mutationAttempted = true
    const publicationResponse = await api.patch(
      `/operations/journeys/${journeyId}/stops/${targetStopId}`,
      {
        data: {
          expectedJourneyRevision: journey.revision,
          platform: requestedPlatform,
        },
      },
    )
    expect(
      publicationResponse.status(),
      'the backend must accept an authorized Controller change',
    ).toBe(200)
    const publishedJourney =
      (await publicationResponse.json()) as JourneySnapshot
    expect(publishedJourney.revision).toBe(journey.revision + 1)
    expect(
      publishedJourney.stops.find((stop) => stop.journeyStopId === targetStopId)
        ?.platform,
    ).toBe(requestedPlatform)
    await refreshedSnapshotResponse

    const finalReadResponse = await api.get(
      `/stations/${stationId}/departures?date=${date}`,
    )
    expect(finalReadResponse.status()).toBe(200)
    const finalDepartures = (await finalReadResponse.json()) as Departure[]
    expect(finalDepartures).toHaveLength(initialDepartures.length)
    for (let index = 0; index < initialDepartures.length; index += 1) {
      const initialItem = initialDepartures[index]
      const finalItem = finalDepartures[index]
      if (
        initialItem.journeyInstanceId === journeyId &&
        initialItem.stationId === stationId &&
        initialItem.stopOrder === before.stopOrder
      ) {
        expect(finalItem.platform).toBe(requestedPlatform)
        expect(finalItem.revision).toBe(journey.revision + 1)
        expect(sameDepartureMeaning(initialItem, finalItem)).toEqual({
          journeyInstanceId: true,
          serviceId: true,
          operationDate: true,
          stationLocalDepartureDate: true,
          status: true,
          stationId: true,
          stopOrder: true,
          scheduledDepartureAt: true,
          operationalDepartureAt: true,
          source: true,
          ambiguity: true,
        })
      } else {
        expect(finalItem).toEqual(initialItem)
      }
    }
    const finalMatches = finalDepartures.filter(
      (item) =>
        item.journeyInstanceId === journeyId &&
        item.stationId === stationId &&
        item.stationLocalDepartureDate === date,
    )
    expect(finalMatches).toHaveLength(1)
    const finalRead = finalMatches[0]
    expect(finalRead.revision).toBe(publishedJourney.revision)
    expect(finalRead.platform).toBe(requestedPlatform)
    expect(sameDepartureMeaning(before, finalRead)).toEqual({
      journeyInstanceId: true,
      serviceId: true,
      operationDate: true,
      stationLocalDepartureDate: true,
      status: true,
      stationId: true,
      stopOrder: true,
      scheduledDepartureAt: true,
      operationalDepartureAt: true,
      source: true,
      ambiguity: true,
    })

    await expect(targetRow).toContainText(requestedPlatform)
    await expect(page.getByText('DEMO DATA — NOT LIVE')).toHaveCount(0)
    await expect(page.locator('body')).not.toContainText(
      /\b(?:within|every)\s+\d+\s+seconds?\b|\bSLA\b/i,
    )
    await testInfo.attach('live-departure-after.json', {
      body: JSON.stringify(
        {
          date,
          stationId,
          journeyId,
          stopId: targetStopId,
          departure: finalRead,
        },
        null,
        2,
      ),
      contentType: 'application/json',
    })
    await testInfo.attach('live-board-after.png', {
      body: await page.screenshot(),
      contentType: 'image/png',
    })
  } catch (error) {
    testFailure = error
  } finally {
    try {
      try {
        if (
          mutationAttempted &&
          targetStopId &&
          originalPlatform !== undefined &&
          requestedPlatform
        ) {
          const currentJourneyResponse = await api.get(`/journeys/${journeyId}`)
          if (currentJourneyResponse.status() !== 200) {
            await recordRestorationFailure(
              testInfo,
              `T030 RESTORATION FAILED. Journey ${journeyId}, stop ${targetStopId}. ` +
                `Could not read current journey revision (HTTP ${currentJourneyResponse.status()}); original test error: ${String(testFailure ?? 'none')}.`,
            )
          }
          const currentJourney =
            (await currentJourneyResponse.json()) as JourneySnapshot
          const currentStop = currentJourney.stops.find(
            (stop) => stop.journeyStopId === targetStopId,
          )
          if (!currentStop) {
            await recordRestorationFailure(
              testInfo,
              `T030 RESTORATION FAILED. Journey ${journeyId}, stop ${targetStopId}. ` +
                `The target stop is absent from revision ${currentJourney.revision}; original test error: ${String(testFailure ?? 'none')}.`,
            )
          }

          if (currentStop!.platform !== originalPlatform) {
            if (currentStop!.platform !== requestedPlatform) {
              await recordRestorationFailure(
                testInfo,
                `T030 RESTORATION FAILED. Journey ${journeyId}, stop ${targetStopId}, revision ${currentJourney.revision}. ` +
                  `Current platform ${JSON.stringify(currentStop!.platform)} differs from the original and test-published values; ` +
                  `manual recovery must inspect the target before publishing. Original test error: ${String(testFailure ?? 'none')}.`,
              )
            }

            const restoreResponse = await api.patch(
              `/operations/journeys/${journeyId}/stops/${targetStopId}`,
              {
                data: {
                  expectedJourneyRevision: currentJourney.revision,
                  platform: originalPlatform,
                },
              },
            )
            if (restoreResponse.status() !== 200) {
              await recordRestorationFailure(
                testInfo,
                `T030 RESTORATION FAILED. Journey ${journeyId}, stop ${targetStopId}, expected revision ${currentJourney.revision}. ` +
                  `Restore PATCH returned HTTP ${restoreResponse.status()}; current platform remains ${JSON.stringify(currentStop!.platform)}. ` +
                  `Original test error: ${String(testFailure ?? 'none')}.`,
              )
            }
          }

          const restoredJourney = await getJourneyForCleanup(
            api,
            testInfo,
            targetStopId,
          )
          const restoredStop = restoredJourney.stops.find(
            (stop) => stop.journeyStopId === targetStopId,
          )
          if (restoredStop?.platform !== originalPlatform) {
            await recordRestorationFailure(
              testInfo,
              `T030 RESTORATION FAILED. Journey ${journeyId}, stop ${targetStopId}, revision ${restoredJourney.revision}. ` +
                `Journey read returned platform ${JSON.stringify(restoredStop?.platform)}; expected ${JSON.stringify(originalPlatform)}. ` +
                `Original test error: ${String(testFailure ?? 'none')}.`,
            )
          }

          const restoredDeparture = await getTargetDepartureForCleanup(
            api,
            departureDate!,
            testInfo,
            targetStopId,
          )
          if (
            restoredDeparture.platform !== originalPlatform ||
            restoredDeparture.revision !== restoredJourney.revision
          ) {
            await recordRestorationFailure(
              testInfo,
              `T030 RESTORATION FAILED. Journey ${journeyId}, stop ${targetStopId}, revision ${restoredJourney.revision}. ` +
                `Station departure projection did not confirm original platform ${JSON.stringify(originalPlatform)}; ` +
                `it returned ${JSON.stringify(restoredDeparture.platform)} at revision ${restoredDeparture.revision}. ` +
                `Original test error: ${String(testFailure ?? 'none')}.`,
            )
          }
        }
      } catch (cleanupError) {
        if (
          cleanupError instanceof Error &&
          cleanupError.message.startsWith('T030 RESTORATION FAILED.')
        ) {
          cleanupFailure = cleanupError.message
        } else {
          cleanupFailure =
            `T030 RESTORATION FAILED. Journey ${journeyId}, stop ${targetStopId ?? 'unresolved'}. ` +
            `Cleanup raised ${String(cleanupError)}; original test error: ${String(testFailure ?? 'none')}.`
          await testInfo.attach('t030-manual-recovery.txt', {
            body: cleanupFailure,
            contentType: 'text/plain',
          })
        }
      }
    } finally {
      await api.dispose()
    }
  }

  if (cleanupFailure) throw new Error(cleanupFailure)
  if (testFailure) throw testFailure
})

async function getJourneyForCleanup(
  api: APIRequestContext,
  testInfo: TestInfo,
  stopId: string,
): Promise<JourneySnapshot> {
  const response = await api.get(`/journeys/${journeyId}`)
  if (response.status() !== 200) {
    await recordRestorationFailure(
      testInfo,
      `T030 RESTORATION FAILED. Journey ${journeyId}, stop ${stopId}. ` +
        `Post-restore journey read returned HTTP ${response.status()}.`,
    )
  }
  return (await response.json()) as JourneySnapshot
}

async function getTargetDepartureForCleanup(
  api: APIRequestContext,
  date: string,
  testInfo: TestInfo,
  stopId: string,
): Promise<Departure> {
  const response = await api.get(
    `/stations/${stationId}/departures?date=${date}`,
  )
  if (response.status() !== 200) {
    await recordRestorationFailure(
      testInfo,
      `T030 RESTORATION FAILED. Journey ${journeyId}, stop ${stopId}. ` +
        `Post-restore station departure read returned HTTP ${response.status()}.`,
    )
  }
  const rows = (await response.json()) as Departure[]
  const targetRows = rows.filter(
    (item) =>
      item.journeyInstanceId === journeyId &&
      item.stationId === stationId &&
      item.stationLocalDepartureDate === date,
  )
  if (targetRows.length !== 1) {
    await recordRestorationFailure(
      testInfo,
      `T030 RESTORATION FAILED. Journey ${journeyId}, stop ${stopId}. ` +
        `Post-restore departure projection returned ${targetRows.length} matching rows for ${date}.`,
    )
  }
  return targetRows[0]
}
