import { expect, test } from '@playwright/test'

test.describe('Station Staff route boundary (local browser, unauthenticated)', () => {
  test('a direct Station Staff URL with selected station values reveals no protected or assigned-station content', async ({
    page,
  }) => {
    await page.goto('/station-staff/operations?station=station-a&resource=platform-1')

    await expect(page.getByRole('alert').filter({ hasText: /authentication is required|sign in/i })).toBeVisible()
    await expect(page.getByRole('heading', { name: /station staff workspace/i })).toHaveCount(0)
    await expect(page.getByText(/station-a|platform-1/i)).toHaveCount(0)
    await expect(page).not.toHaveURL(/\/controller/)
  })

  test('a direct Controller URL remains closed without an authenticated identity', async ({ page }) => {
    await page.goto('/controller/operations')

    await expect(page.getByRole('alert').filter({ hasText: /authentication is required|sign in/i })).toBeVisible()
    await expect(page.getByRole('heading', { name: /controller workspace/i })).toHaveCount(0)
  })
})
