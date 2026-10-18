import { expect, test } from '@playwright/test'

const stationId = '00000000-0000-4000-8000-000000000101'

test('retains fixture departures with an accessible stale warning offline, then shows recovery after source revalidation', async ({ page, context }) => {
  await page.route('**/api/stations', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ items: [{ stationId, name: 'DEMO Central Station' }] }),
    })
  })

  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'DEMO Central Station' })).toBeVisible()
  await expect(page.getByText('DEMO DATA — NOT LIVE')).toBeVisible()
  await expect(page.getByText('DEMO TEST Service A')).toBeVisible()

  await context.setOffline(true)
  await page.evaluate(() => window.dispatchEvent(new Event('offline')))
  await expect(page.getByText('Departure information may be outdated.')).toBeVisible()
  await expect(page.getByText('DEMO TEST Service A')).toBeVisible()

  await context.setOffline(false)
  await page.evaluate(() => window.dispatchEvent(new Event('online')))
  await expect(page.getByText('Departure information has been revalidated.')).toBeVisible()
  await expect(page.getByText('DEMO TEST Service A')).toBeVisible()
  await expect(page.getByText('DEMO DATA — NOT LIVE')).toBeVisible()
  await expect(page.getByText('Departure information may be outdated.')).toHaveCount(0)
})
