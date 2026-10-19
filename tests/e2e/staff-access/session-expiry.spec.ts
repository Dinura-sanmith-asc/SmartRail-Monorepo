import { createSign, generateKeyPairSync } from 'node:crypto'
import { expect, test, type Page, type Route } from '@playwright/test'

const issuer = process.env.VITE_COGNITO_ISSUER ?? 'https://issuer.local.test/pool'
const loginDomain = process.env.VITE_COGNITO_MANAGED_LOGIN_DOMAIN ?? 'https://login.local.test'
const clientId = process.env.VITE_COGNITO_APP_CLIENT_ID ?? 'local-public-spa-client'
const callbackUri = process.env.VITE_COGNITO_CALLBACK_URI ?? 'http://127.0.0.1:4173/auth/callback'
const apiBaseUrl = process.env.VITE_SMARTRAIL_API_BASE_URL ?? 'https://api.local.test'
const { privateKey, publicKey } = generateKeyPairSync('rsa', { modulusLength: 2048 })
const publicJwk = publicKey.export({ format: 'jwk' })
const keyId = 'session-expiry-fixture-key'

function base64Url(value: string | Buffer) {
  return Buffer.from(value).toString('base64url')
}

function signIdToken(nonce: string) {
  const header = base64Url(JSON.stringify({ alg: 'RS256', kid: keyId, typ: 'JWT' }))
  const now = Math.floor(Date.now() / 1000)
  const claims = base64Url(
    JSON.stringify({ iss: issuer, sub: 'expiry-fixture-subject', aud: clientId, iat: now, exp: now + 600, nonce }),
  )
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

async function installSessionFixture(page: Page, accessTokenLifetime = 600) {
  let tokenExchangeCount = 0
  let identityRequestCount = 0
  let codeVerifier: string | null = null
  let nonceForToken = ''

  await page.addInitScript(() => {
    const originalSetItem = Storage.prototype.setItem
    Storage.prototype.setItem = function (key: string, value: string) {
      const result = originalSetItem.call(this, key, value)
      if (this === window.sessionStorage) {
        const saved = JSON.parse(window.name || '{"transactions":[]}') as { transactions: Array<[string, string]> }
        saved.transactions.push([key, value])
        window.name = JSON.stringify(saved)
      }
      return result
    }
  })

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
    const authorizationUrl = new URL(route.request().url())
    const redirectUri = authorizationUrl.searchParams.get('redirect_uri') ?? callbackUri
    const state = authorizationUrl.searchParams.get('state') ?? ''
    nonceForToken = authorizationUrl.searchParams.get('nonce') ?? ''
    const callbackUrl = new URL(redirectUri)
    callbackUrl.searchParams.set('code', 'session-fixture-code')
    callbackUrl.searchParams.set('state', state)
    await route.fulfill({ status: 302, headers: { location: callbackUrl.toString() } })
  })
  await page.route('**/oauth2/token', async (route: Route) => {
    tokenExchangeCount += 1
    const form = new URLSearchParams(route.request().postData() ?? '')
    codeVerifier = form.get('code_verifier')
    await route.fulfill({
      status: 200,
      headers: corsHeaders(),
      body: JSON.stringify({
        access_token: 'fixture-access-token',
        id_token: signIdToken(nonceForToken),
        token_type: 'Bearer',
        expires_in: accessTokenLifetime,
        scope: 'openid email',
      }),
    })
  })
  await page.route(`${apiBaseUrl}/api/me`, async (route: Route) => {
    identityRequestCount += 1
    await route.fulfill({
      status: 200,
      headers: corsHeaders(),
      body: JSON.stringify({ subject: 'expiry-fixture-subject', roles: ['Controller'] }),
    })
  })

  return {
    readTransactions: () => page.evaluate(() => {
      try {
        const transactions = (JSON.parse(window.name || '{"transactions":[]}') as { transactions: Array<[string, string]> }).transactions
        window.name = ''
        return transactions
      } catch {
        window.name = ''
        return []
      }
    }),
    get tokenExchangeCount() { return tokenExchangeCount },
    get identityRequestCount() { return identityRequestCount },
    get codeVerifier() { return codeVerifier },
  }
}

async function signIn(page: Page) {
  await page.goto('/')
  await page.getByRole('button', { name: /sign in/i }).click()
}

async function expectNoControllerContent(page: Page) {
  await expect(page.getByRole('heading', { name: /controller workspace/i })).toHaveCount(0)
  await expect(page.getByText(/authorized controller access is active/i)).toHaveCount(0)
}

test.describe('session expiry and browser storage (mocked local fixture)', () => {
  test('keeps tokens volatile and clears temporary OAuth transaction state after callback', async ({ page }) => {
    const fixture = await installSessionFixture(page)
    const browserConsole: string[] = []
    page.on('console', (message) => browserConsole.push(message.text()))
    await signIn(page)

    await expect(page.getByRole('heading', { name: /controller workspace/i })).toBeVisible()
    expect(fixture.tokenExchangeCount).toBe(1)
    expect(fixture.codeVerifier).toBeTruthy()
    const transaction = JSON.stringify(await fixture.readTransactions()).toLowerCase()
    expect(transaction).not.toBe('[]')
    expect(transaction).toMatch(/code_verifier/)
    expect(transaction).not.toMatch(/access_token|id_token|refresh_token/)
    await expect.poll(() => page.evaluate(() => sessionStorage.length)).toBe(0)
    expect(await page.evaluate(() => localStorage.length)).toBe(0)
    expect(await page.evaluate(() => document.cookie)).toBe('')
    expect(await page.evaluate(() => caches.keys())).toEqual([])
    expect(await page.evaluate(() => indexedDB.databases())).toEqual([])
    expect(browserConsole.join('\n')).not.toContain('fixture-access-token')

    const currentUrl = new URL(page.url())
    expect(currentUrl.searchParams.has('code')).toBe(false)
    expect(currentUrl.searchParams.has('state')).toBe(false)
    expect(currentUrl.href).not.toContain('fixture-access-token')
    await page.goBack()
    await page.goForward()
    expect(new URL(page.url()).searchParams.has('code')).toBe(false)
    expect(new URL(page.url()).searchParams.has('state')).toBe(false)
  })

  test('requires sign-in again after a full page reload with no in-memory session', async ({ page }) => {
    const fixture = await installSessionFixture(page)
    await signIn(page)
    await expect(page.getByRole('heading', { name: /controller workspace/i })).toBeVisible()
    await fixture.readTransactions()

    await page.reload()

    await expect(page.getByRole('link', { name: /sign in/i })).toBeVisible()
    await expectNoControllerContent(page)
    expect(fixture.identityRequestCount).toBe(1)
    expect(fixture.tokenExchangeCount).toBe(1)
  })

  test('closes protected content on token expiry without replaying the identity request', async ({ page }) => {
    const fixture = await installSessionFixture(page, 2)
    await signIn(page)
    await expect(page.getByRole('heading', { name: /controller workspace/i })).toBeVisible()

    await expect(page.getByRole('link', { name: /sign in/i })).toBeVisible({ timeout: 10_000 })
    await expectNoControllerContent(page)
    expect(fixture.identityRequestCount).toBe(1)
    expect(fixture.tokenExchangeCount).toBe(1)
  })
})
