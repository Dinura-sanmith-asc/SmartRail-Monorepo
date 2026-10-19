import { expect, test } from '@playwright/test'
import { openStationDisplay } from './fixtures/station-display'

const profiles = [
  { name: '1920x1080', width: 1920, height: 1080 },
  { name: '1366x768', width: 1366, height: 768 },
]

for (const profile of profiles) {
  test(`preserves all required populated board information at ${profile.name}`, async ({ page }, testInfo) => {
    await page.setViewportSize({ width: profile.width, height: profile.height })
    await openStationDisplay(page, { platformChangeNotice: true })
    const delayedDeparture = page.getByRole('article', { name: 'DEMO TEST Service A departure' })
    const cancelledDeparture = page.getByRole('article', { name: 'DEMO TEST Service B departure' })

    await expect(page.getByRole('heading', { name: 'DEMO Central Station', level: 1 })).toBeVisible()
    await expect(delayedDeparture).toBeVisible()
    await expect(cancelledDeparture).toBeVisible()
    await expect(delayedDeparture.getByText('08:15')).toBeVisible()
    await expect(cancelledDeparture.getByText('08:40')).toBeVisible()
    await expect(delayedDeparture.getByText('Platform', { exact: true })).toBeVisible()
    await expect(delayedDeparture.getByText('DEMO platform change notice: platform 2')).toBeVisible()
    await expect(delayedDeparture.getByText('Delay', { exact: true })).toBeVisible()
    await expect(delayedDeparture.getByText('DEMO delay example')).toBeVisible()
    await expect(cancelledDeparture.getByRole('status', { name: 'Cancellation status: Cancelled' })).toBeVisible()
    await expect(page.getByText('DEMO DATA — NOT LIVE')).toBeVisible()

    const dimensions = await page.evaluate(() => ({
      clientWidth: document.documentElement.clientWidth,
      scrollWidth: document.documentElement.scrollWidth,
    }))
    expect(dimensions.scrollWidth).toBeLessThanOrEqual(dimensions.clientWidth)
    for (const requiredText of [
      '08:15',
      '08:40',
      'DEMO TEST Service A',
      'DEMO TEST Service B',
      'DEMO delay example',
      'DEMO platform change notice: platform 2',
      'DEMO DATA — NOT LIVE',
    ]) {
      await expect(page.getByText(requiredText)).toBeInViewport()
    }
    await expect(cancelledDeparture.getByRole('status', { name: 'Cancellation status: Cancelled' })).toBeInViewport()
    await page.screenshot({ path: testInfo.outputPath(`station-board-${profile.name}.png`), fullPage: true })
  })
}
