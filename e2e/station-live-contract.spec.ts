import { expect, test } from '@playwright/test'

const stationId = process.env.STATION_DISPLAY_LIVE_STATION_ID ?? process.env.VITE_STATION_ID
const apiBaseUrl = process.env.STATION_DISPLAY_LIVE_API_BASE_URL ?? process.env.VITE_API_BASE_URL

function stationLocalDate(): string {
  const parts = new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Asia/Colombo', year: 'numeric', month: '2-digit', day: '2-digit',
  }).formatToParts(new Date())
  const get = (type: string) => parts.find((part) => part.type === type)?.value
  return `${get('year')}-${get('month')}-${get('day')}`
}

test.describe('live Journeys & Status contract', () => {
  test.beforeEach(() => {
    if (!stationId || !apiBaseUrl) throw new Error('Live acceptance requires a configured station ID and API base URL.')
  })

  test('renders the configured station and its real dated departure projection', async ({ page }) => {
    const departuresResponsePromise = page.waitForResponse((response) =>
      response.url().includes(`/stations/${stationId}/departures?date=`),
    )
    await page.goto('/')

    const response = await departuresResponsePromise
    expect(response.status()).toBe(200)
    const departures: unknown = await response.json()
    expect(Array.isArray(departures)).toBe(true)
    await expect(page.getByText('DEMO DATA — NOT LIVE')).toHaveCount(0)
    await expect(page.getByRole('heading', { name: 'Departures' })).toBeVisible()

    if ((departures as Array<Record<string, unknown>>).length === 0) {
      await expect(page.getByText('No departures are currently available.')).toBeVisible()
      return
    }

    const rows = departures as Array<Record<string, unknown>>
    for (const item of rows) {
      expect(item.stationId).toBe(stationId)
      expect(item.stationLocalDepartureDate).toBe(stationLocalDate())
      await expect(page.getByText(`Service ID ${item.serviceId}`)).toBeVisible()
      await expect(page.getByText(String(item.status), { exact: true }).first()).toBeVisible()
    }
    const renderedServiceIds = await page.locator('.departure-row__identity h3').allTextContents()
    expect(renderedServiceIds).toEqual(rows.map((item) =>
      `Service ID ${item.serviceId}${item.ambiguity === 'ambiguous' ? ' — ambiguous identity' : ''}`,
    ))
  })

  test('the backend rejects malformed dates and unpublished station IDs using Problem responses', async ({ request }) => {
    const malformed = await request.get(`${apiBaseUrl}/stations/${stationId}/departures?date=2026-2-01`)
    expect(malformed.status()).toBe(400)
    expect(malformed.headers()['content-type']).toContain('application/problem+json')

    const unpublishedId = crypto.randomUUID()
    const missing = await request.get(`${apiBaseUrl}/stations/${unpublishedId}/departures?date=${stationLocalDate()}`)
    expect(missing.status()).toBe(404)
    expect(missing.headers()['content-type']).toContain('application/problem+json')
  })
})
