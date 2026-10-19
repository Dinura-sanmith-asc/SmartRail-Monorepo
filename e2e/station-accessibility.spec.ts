import AxeBuilder from '@axe-core/playwright'
import { expect, test } from '@playwright/test'
import { openStationDisplay, setSourceAvailability } from './fixtures/station-display'

const profiles = [
  { name: '1920x1080', width: 1920, height: 1080 },
  { name: '1366x768', width: 1366, height: 768 },
]

for (const profile of profiles) {
  test(`has accessible information hierarchy and text status cues at ${profile.name}`, async ({ page }) => {
    await page.setViewportSize({ width: profile.width, height: profile.height })
    await openStationDisplay(page, { platformChangeNotice: true })
    const delayedDeparture = page.getByRole('article', { name: 'DEMO TEST Service A departure' })
    const cancelledDeparture = page.getByRole('article', { name: 'DEMO TEST Service B departure' })

    await expect(page.getByRole('heading', { level: 1, name: 'DEMO Central Station' })).toBeVisible()
    await expect(page.getByRole('heading', { level: 2, name: 'Departures' })).toBeVisible()
    await expect(page.getByRole('heading', { level: 3, name: 'DEMO TEST Service A' })).toBeVisible()
    await expect(delayedDeparture.getByText('Status', { exact: true })).toBeVisible()
    await expect(delayedDeparture.getByText('DEMO delayed', { exact: true })).toBeVisible()
    await expect(delayedDeparture.getByText('Platform', { exact: true })).toBeVisible()
    await expect(delayedDeparture.getByText('DEMO platform change notice: platform 2', { exact: true })).toBeVisible()
    await expect(cancelledDeparture.getByRole('status', { name: 'Cancellation status: Cancelled' })).toBeVisible()
    await expect(cancelledDeparture.getByText('Unknown', { exact: true })).toBeVisible()
    await expect(cancelledDeparture.getByText('Unavailable', { exact: true })).toBeVisible()

    const results = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'])
      .analyze()
    expect(results.violations, JSON.stringify(results.violations, null, 2)).toEqual([])
  })
}

test('announces stale and unavailable states accessibly', async ({ page }) => {
  await openStationDisplay(page)
  await setSourceAvailability(page, false)
  await page.evaluate(() => window.dispatchEvent(new Event('offline')))
  await expect(page.getByText('Departure information may be outdated.')).toBeVisible()
  const staleResults = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa']).analyze()
  expect(staleResults.violations, JSON.stringify(staleResults.violations, null, 2)).toEqual([])

  const unavailablePage = await page.context().newPage()
  await openStationDisplay(unavailablePage, { sourceAvailable: false })
  await expect(unavailablePage.getByRole('alert')).toHaveText('Departure information is unavailable.')
  const unavailableResults = await new AxeBuilder({ page: unavailablePage })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'])
    .analyze()
  expect(unavailableResults.violations, JSON.stringify(unavailableResults.violations, null, 2)).toEqual([])
  await unavailablePage.close()
})
