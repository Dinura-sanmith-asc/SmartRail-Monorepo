import { expect, type Page, test } from '@playwright/test'

const stationId = '00000000-0000-4000-8000-000000000101'
const stationName = 'DEMO Central Station'

async function prepareDisplay(page: Page, sourceAvailable = true) {
  await page.addInitScript((available) => {
    ;(window as Window & { __stationDisplayE2E?: unknown }).__stationDisplayE2E = {
      sourceAvailable: available,
      holdNextRevalidation: false,
      revalidationCalls: 0,
    }
  }, sourceAvailable)
  await page.route('**/api/stations', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ items: [{ stationId, name: stationName }] }),
    })
  })
  await page.goto('/')
  await expect(page.getByRole('heading', { name: stationName })).toBeVisible()
}

async function setSourceAvailable(page: Page, value: boolean) {
  await page.evaluate((available) => {
    const control = (window as Window & { __stationDisplayE2E: { sourceAvailable: boolean } }).__stationDisplayE2E
    control.sourceAvailable = available
  }, value)
}

async function dispatchConnectivity(page: Page, eventName: 'offline' | 'online') {
  await page.evaluate((name) => window.dispatchEvent(new Event(name)), eventName)
}

test('shows unavailable with no fabricated or cached departures when offline before initial data', async ({ page }) => {
  await prepareDisplay(page, false)

  await expect(page.getByRole('alert')).toHaveText('Departure information is unavailable.')
  await expect(page.getByRole('list', { name: 'Station departures' })).toHaveCount(0)
  await expect(page.getByText('DEMO DATA — NOT LIVE')).toHaveCount(0)
  await expect(page.getByText(/current|live/i)).toHaveCount(0)
})

test('retains the last successful fixture with a stale warning when the source goes offline', async ({ page }) => {
  await prepareDisplay(page)
  await expect(page.getByText('DEMO DATA — NOT LIVE')).toBeVisible()
  await expect(page.getByText('DEMO TEST Service A')).toBeVisible()

  await setSourceAvailable(page, false)
  await dispatchConnectivity(page, 'offline')

  await expect(page.getByText('Departure information may be outdated.')).toBeVisible()
  await expect(page.getByText('DEMO TEST Service A')).toBeVisible()
  await expect(page.getByText('DEMO DATA — NOT LIVE')).toBeVisible()
  await expect(page.getByText('Departure information has been revalidated.')).toHaveCount(0)
})

test('keeps stale data through failed revalidation and ignores overlapping online notifications', async ({ page }) => {
  await prepareDisplay(page)
  await expect(page.getByText('DEMO TEST Service A')).toBeVisible()
  await setSourceAvailable(page, false)
  await dispatchConnectivity(page, 'offline')
  await expect(page.getByText('Departure information may be outdated.')).toBeVisible()

  await page.evaluate(() => {
    const control = (window as Window & { __stationDisplayE2E: { sourceAvailable: boolean; holdNextRevalidation: boolean } }).__stationDisplayE2E
    control.sourceAvailable = true
    control.holdNextRevalidation = true
    window.dispatchEvent(new Event('online'))
    window.dispatchEvent(new Event('online'))
  })

  await expect(page.getByText('Checking departure information. Displayed information may be outdated.')).toBeVisible()
  await expect(page.getByText('DEMO TEST Service A')).toBeVisible()
  await expect.poll(() => page.evaluate(() => (window as Window & { __stationDisplayE2E: { revalidationCalls: number } }).__stationDisplayE2E.revalidationCalls)).toBe(1)

  await page.evaluate(() => window.dispatchEvent(new CustomEvent('station-display:e2e-resolve-revalidation', { detail: { ok: false } })))
  await expect(page.getByText('Departure information may be outdated.')).toBeVisible()
  await expect(page.getByText('DEMO TEST Service A')).toBeVisible()
  await expect(page.getByText('Departure information has been revalidated.')).toHaveCount(0)
})

test('clears stale only after successful revalidation returns the new fixture snapshot', async ({ page }) => {
  await prepareDisplay(page)
  await expect(page.getByText('DEMO TEST Service A')).toBeVisible()
  await setSourceAvailable(page, false)
  await dispatchConnectivity(page, 'offline')
  await expect(page.getByText('Departure information may be outdated.')).toBeVisible()

  await page.evaluate(() => {
    const control = (window as Window & { __stationDisplayE2E: { sourceAvailable: boolean; holdNextRevalidation: boolean } }).__stationDisplayE2E
    control.sourceAvailable = true
    control.holdNextRevalidation = true
    window.dispatchEvent(new Event('online'))
  })
  await expect(page.getByText('Checking departure information. Displayed information may be outdated.')).toBeVisible()
  await expect(page.getByText('DEMO TEST Service A')).toBeVisible()

  await page.evaluate(() => window.dispatchEvent(new CustomEvent('station-display:e2e-resolve-revalidation', {
    detail: { ok: true, serviceLabel: 'DEMO TEST Service A — UPDATED' },
  })))

  await expect(page.getByText('DEMO TEST Service A — UPDATED')).toBeVisible()
  await expect(page.getByText('Departure information has been revalidated.')).toBeVisible()
  await expect(page.getByText('Departure information may be outdated.')).toHaveCount(0)
  await expect(page.getByText('DEMO DATA — NOT LIVE')).toBeVisible()
})
