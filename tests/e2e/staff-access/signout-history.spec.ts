import { createSign, generateKeyPairSync } from 'node:crypto'
import { expect, test, type Page, type Route } from '@playwright/test'

const issuer = process.env.VITE_COGNITO_ISSUER ?? 'https://issuer.local.test/pool'
const loginDomain = process.env.VITE_COGNITO_MANAGED_LOGIN_DOMAIN ?? 'https://login.local.test'
const clientId = process.env.VITE_COGNITO_APP_CLIENT_ID ?? 'local-public-spa-client'
const callbackUri = process.env.VITE_COGNITO_CALLBACK_URI ?? 'http://127.0.0.1:4173/auth/callback'
const logoutUri = process.env.VITE_COGNITO_LOGOUT_URI ?? 'http://127.0.0.1:4173/'
const apiBaseUrl = process.env.VITE_SMARTRAIL_API_BASE_URL ?? 'https://api.local.test'
const { privateKey, publicKey } = generateKeyPairSync('rsa', { modulusLength: 2048 })
const publicJwk = publicKey.export({ format: 'jwk' })
const keyId = 'signout-history-fixture-key'

function base64Url(value: string | Buffer) {
  return Buffer.from(value).toString('base64url')
}

function signIdToken(nonce: string) {
  const header = base64Url(JSON.stringify({ alg: 'RS256', kid: keyId, typ: 'JWT' }))
  const now = Math.floor(Date.now() / 1000)
  const claims = base64Url(JSON.stringify({ iss: issuer, sub: 'signout-fixture-subject', aud: clientId, iat: now, exp: now + 600, nonce }))
  const signingInput = `${header}.${claims}`
  const signer = createSign('RSA-SHA256')
  signer.update(signingInput)
  signer.end()
  return `${signingInput}.${signer.sign(privateKey).toString('base64url')}`
}

function corsHeaders() {
  return {
    'access-control-allow-origin': '*',
    'access-control-allow-methods': 'GET, POST, OPTIONS',
    'access-control-allow-headers': 'authorization, content-type',
    'content-type': 'application/json',
  }
}

async function installSignInFixture(page: Page, roles: string[] = ['Controller']) {
  let identityRequestCount = 0
  let nonceForToken = ''

  await page.route('**/.well-known/openid-configuration', async (route: Route) => {
    await route.fulfill({
      status: 200,
      headers: corsHeaders(),
      body: JSON.stringify({
        issuer,
        authorization_endpoint: `${loginDomain}/oauth2/authorize`,
        token_endpoint: `${loginDomain}/oauth2/token`,
        jwks_uri: `${issuer}/.well-known/jwks.json`,
      }),
    })
  })
  await page.route('**/.well-known/jwks.json', async (route: Route) => {
    await route.fulfill({
      status: 200,
      headers: corsHeaders(),
      body: JSON.stringify({ keys: [{ ...publicJwk, kid: keyId, use: 'sig', alg: 'RS256' }] }),
    })
  })
  await page.route('**/oauth2/authorize**', async (route: Route) => {
    const authorizeUrl = new URL(route.request().url())
    const redirectUri = authorizeUrl.searchParams.get('redirect_uri') ?? callbackUri
    const callback = new URL(redirectUri)
    callback.searchParams.set('code', 'signout-fixture-code')
    callback.searchParams.set('state', authorizeUrl.searchParams.get('state') ?? '')
    nonceForToken = authorizeUrl.searchParams.get('nonce') ?? ''
    await route.fulfill({ status: 302, headers: { location: callback.toString() } })
  })
  await page.route('**/oauth2/token', async (route: Route) => {
    await route.fulfill({
      status: 200,
      headers: corsHeaders(),
      body: JSON.stringify({
        access_token: 'signout-fixture-access-token',
        id_token: signIdToken(nonceForToken),
        token_type: 'Bearer',
        expires_in: 600,
        scope: 'openid email',
      }),
    })
  })
  await page.route(`${apiBaseUrl}/api/me`, async (route: Route) => {
    identityRequestCount += 1
    await route.fulfill({
      status: 200,
      headers: corsHeaders(),
      body: JSON.stringify({ subject: 'signout-fixture-subject', roles }),
    })
  })

  return { get identityRequestCount() { return identityRequestCount } }
}

async function signInAsController(page: Page) {
  await page.goto('/')
  await page.getByRole('button', { name: /sign in/i }).click()
  await expect(page.getByRole('heading', { name: /controller workspace/i })).toBeVisible()
}

async function signInAsStationStaff(page: Page) {
  await page.goto('/')
  await page.getByRole('button', { name: /sign in/i }).click()
  await expect(page.getByRole('heading', { name: /station staff workspace/i })).toBeVisible()
}

async function expectSignedOut(page: Page) {
  await expect(page.getByRole('heading', { name: /controller workspace/i })).toHaveCount(0)
  await expect(page.getByRole('heading', { name: /station staff workspace/i })).toHaveCount(0)
  await expect(page.getByText(/authorized controller access is active/i)).toHaveCount(0)
}

test.describe('sign-out and browser history (mocked local fixtures)', () => {
  test('clears local access, navigates through configured Cognito logout, and stays signed out on Back and direct URLs', async ({ page }) => {
    const fixture = await installSignInFixture(page)
    let remoteLogoutUrl: URL | undefined
    await page.route(`${loginDomain}/logout**`, async (route: Route) => {
      remoteLogoutUrl = new URL(route.request().url())
      await route.fulfill({ status: 200, contentType: 'text/html', body: '<title>Local logout fixture</title><p>Signed out</p>' })
    })

    await signInAsController(page)
    const identityRequestsBeforeLogout = fixture.identityRequestCount
    const signOutButton = page.getByRole('button', { name: /sign out/i })
    await expect(signOutButton).toBeVisible()
    await signOutButton.focus()
    await page.keyboard.press('Enter')

    await expect.poll(() => remoteLogoutUrl?.pathname).toBe('/logout')
    expect(remoteLogoutUrl?.searchParams.get('client_id')).toBe(clientId)
    expect(remoteLogoutUrl?.searchParams.get('logout_uri')).toBe(logoutUri)
    expect(remoteLogoutUrl?.searchParams.has('post_logout_redirect_uri')).toBe(false)
    await expect(page.getByText('Signed out')).toBeVisible()

    await page.goBack()
    await expectSignedOut(page)
    await expect(page.getByRole('button', { name: /sign in/i })).toBeVisible()
    expect(await page.evaluate(() => localStorage.length)).toBe(0)
    expect(await page.evaluate(() => sessionStorage.length)).toBe(0)
    expect(await page.evaluate(() => document.cookie)).toBe('')
    expect(await page.evaluate(() => caches.keys())).toEqual([])
    expect(page.url()).not.toMatch(/access_token|id_token|refresh_token/i)
    await page.goForward()
    await page.goto('http://127.0.0.1:4173/controller')
    await expect(page.getByRole('alert').filter({ hasText: /authentication is required|sign in/i })).toBeVisible()
    await expectSignedOut(page)
    expect(fixture.identityRequestCount).toBe(identityRequestsBeforeLogout)
  })

  test('keeps local access closed if remote navigation fails and browser Back returns to sign-in', async ({ page }) => {
    const fixture = await installSignInFixture(page)
    await page.route(`${loginDomain}/logout**`, async (route: Route) => route.abort('failed'))

    await signInAsController(page)
    const identityRequestsBeforeLogout = fixture.identityRequestCount
    await page.getByRole('button', { name: /sign out/i }).click()

    // A failed top-level cross-origin navigation is displayed by the browser;
    // returning to the replaced local entry must not restore the old session.
    await page.goBack()
    await expect(page.getByRole('heading', { name: /staff sign in/i })).toBeVisible()
    await expectSignedOut(page)
    expect(fixture.identityRequestCount).toBe(identityRequestsBeforeLogout)

    await page.goto('http://127.0.0.1:4173/controller')
    await expect(page.getByRole('alert').filter({ hasText: /authentication is required|sign in/i })).toBeVisible()
    await expectSignedOut(page)
    expect(fixture.identityRequestCount).toBe(identityRequestsBeforeLogout)
  })

  test('Station Staff can sign out and cannot restore the shell from a direct URL', async ({ page }) => {
    const fixture = await installSignInFixture(page, ['Station Staff'])
    await page.route(`${loginDomain}/logout**`, async (route: Route) => {
      await route.fulfill({ status: 200, contentType: 'text/html', body: '<title>Local logout fixture</title><p>Signed out</p>' })
    })
    await signInAsStationStaff(page)
    const identityRequestsBeforeLogout = fixture.identityRequestCount
    await page.getByRole('button', { name: /sign out/i }).click()
    await expect(page.getByText('Signed out')).toBeVisible()
    await page.goBack()
    await expect(page.getByRole('heading', { name: /staff sign in/i })).toBeVisible()
    await page.goto('http://127.0.0.1:4173/station-staff')
    await expectSignedOut(page)
    expect(fixture.identityRequestCount).toBe(identityRequestsBeforeLogout)
  })

  test('multi-role navigation offers sign-out once and closes both role shells', async ({ page }) => {
    const fixture = await installSignInFixture(page, ['Controller', 'Station Staff'])
    await page.route(`${loginDomain}/logout**`, async (route: Route) => {
      await route.fulfill({ status: 200, contentType: 'text/html', body: '<title>Local logout fixture</title><p>Signed out</p>' })
    })
    await signInAsController(page)
    await expect(page.getByRole('link', { name: /station staff workspace/i })).toBeVisible()
    const identityRequestsBeforeLogout = fixture.identityRequestCount
    await page.getByRole('button', { name: /sign out/i }).click()
    await expect(page.getByText('Signed out')).toBeVisible()
    await page.goBack()
    await expect(page.getByRole('heading', { name: /staff sign in/i })).toBeVisible()
    await page.goto('http://127.0.0.1:4173/controller')
    await expectSignedOut(page)
    await page.goto('http://127.0.0.1:4173/station-staff')
    await expectSignedOut(page)
    expect(fixture.identityRequestCount).toBe(identityRequestsBeforeLogout)
  })
})
