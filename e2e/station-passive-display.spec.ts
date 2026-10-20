import { expect, test } from '@playwright/test'
import { openStationDisplay, setSourceAvailability } from './fixtures/station-display'

async function expectNoPassengerActions(page: import('@playwright/test').Page) {
  await expect(page.locator('button, input, select:not([aria-label="Language"]), textarea, a[href], [role="button"], [role="link"], [role="combobox"]:not([aria-label="Language"])')).toHaveCount(0)
  await expect(page.getByRole('combobox', { name: 'Language' })).toHaveCount(1)
  await expect(page.getByText(/station management|controller action|publish|edit railway|sign in|log in|authentication|station terminal|kiosk/i)).toHaveCount(0)
}

test('keeps populated display passive', async ({ page }) => {
  await openStationDisplay(page)
  await expect(page.getByText('DEMO TEST Service A')).toBeVisible()
  await expectNoPassengerActions(page)
})

test('keeps unavailable display passive', async ({ page }) => {
  await openStationDisplay(page, { sourceAvailable: false })
  await expect(page.getByRole('alert')).toHaveText('Departure information is unavailable.')
  await expectNoPassengerActions(page)
})

test('keeps stale display passive', async ({ page }) => {
  await openStationDisplay(page)
  await expect(page.getByText('DEMO TEST Service A')).toBeVisible()
  await setSourceAvailability(page, false)
  await page.evaluate(() => window.dispatchEvent(new Event('offline')))
  await expect(page.getByText('Departure information may be outdated.')).toBeVisible()
  await expectNoPassengerActions(page)
})
