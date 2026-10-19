import { expect, type Page } from '@playwright/test'

export const configuredStationId = '10000000-0000-0000-0000-000000000001'
export const configuredStationName = 'DEMO Central Station'

export async function openStationDisplay(
  page: Page,
  options: { sourceAvailable?: boolean; platformChangeNotice?: boolean } = {},
) {
  await page.addInitScript((controlOptions) => {
    ;(window as Window & { __stationDisplayE2E?: unknown }).__stationDisplayE2E = {
      sourceAvailable: controlOptions.sourceAvailable,
      platformChangeNotice: controlOptions.platformChangeNotice,
      holdNextRevalidation: false,
      revalidationCalls: 0,
    }
  }, {
    sourceAvailable: options.sourceAvailable ?? true,
    platformChangeNotice: options.platformChangeNotice ?? false,
  })
  await page.route('**/api/stations', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ items: [{ stationId: configuredStationId, name: configuredStationName }] }),
    })
  })
  await page.goto('/')
  await expect(page.getByRole('heading', { name: configuredStationName, level: 1 })).toBeVisible()
}

export async function setSourceAvailability(page: Page, available: boolean) {
  await page.evaluate((nextValue) => {
    const control = (window as Window & { __stationDisplayE2E: { sourceAvailable: boolean } }).__stationDisplayE2E
    control.sourceAvailable = nextValue
  }, available)
}
