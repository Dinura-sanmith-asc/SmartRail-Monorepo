import { createHash, createSign, generateKeyPairSync } from 'node:crypto'
import { expect, test, type Page, type Route } from '@playwright/test'

type CallbackCase = 'valid' | 'missing-state' | 'invalid-state' | 'missing-code' | 'invalid-code' | 'cancelled'

const issuer = process.env.VITE_COGNITO_ISSUER ?? 'https://issuer.local.test/pool'
const managedLoginDomain = process.env.VITE_COGNITO_MANAGED_LOGIN_DOMAIN ?? 'https://login.local.test'
const clientId = process.env.VITE_COGNITO_APP_CLIENT_ID ?? 'local-public-spa-client'
const callbackUri = process.env.VITE_COGNITO_CALLBACK_URI ?? 'http://127.0.0.1:4173/auth/callback'
const apiBaseUrl = process.env.VITE_SMARTRAIL_API_BASE_URL ?? 'https://api.local.test'

const { privateKey, publicKey } = generateKeyPairSync('rsa', { modulusLength: 2048 })
const publicJwk = publicKey.export({ format: 'jwk' })
const keyId = 'local-oidc-fixture-key'

function base64Url(value: string | Buffer): string {
  return Buffer.from(value).toString('base64url')
}

function signIdToken(nonce: string): string {
  const header = base64Url(JSON.stringify({ alg: 'RS256', kid: keyId, typ: 'JWT' }))
  const now = Math.floor(Date.now() / 1000)
  const claims = base64Url(
    JSON.stringify({
      iss: issuer,
      sub: 'fixture-cognito-subject',
      aud: clientId,
      iat: now,
      exp: now + 600,
      nonce,
    }),
  )
  const signingInput = `${header}.${claims}`
  const signer = createSign('RSA-SHA256')
  signer.update(signingInput)
  signer.end()
  return `${signingInput}.${signer.sign(privateKey).toString('base64url')}`
}

function corsHeaders(origin = '*') {
  return {
    'access-control-allow-origin': origin,
    'access-control-allow-methods': 'GET, POST, OPTIONS',
    'access-control-allow-headers': 'authorization, content-type',
    'content-type': 'application/json',
  }
}

async function installLocalOidcFixture(page: Page, callbackCase: CallbackCase, accessTokenLifetime = 600) {
  let authorizeCount = 0
  let tokenExchangeCount = 0
  let callbackUrl: string | undefined
  let receivedCodeVerifier: string | null = null
  let receivedCodeChallenge: string | null = null
  let receivedNonce: string | null = null

  await page.route('**/.well-known/openid-configuration', async (route: Route) => {
    await route.fulfill({
      status: 200,
      headers: corsHeaders(),
      body: JSON.stringify({
        issuer,
        authorization_endpoint: `${managedLoginDomain}/oauth2/authorize`,
        token_endpoint: `${managedLoginDomain}/oauth2/token`,
        jwks_uri: `${issuer}/.well-known/jwks.json`,
      }),
    })
  })

  await page.route('**/.well-known/jwks.json', async (route: Route) => {
    await route.fulfill({
      status: 200,
      headers: corsHeaders(),
      body: JSON.stringify({
        keys: [{ ...publicJwk, kid: keyId, use: 'sig', alg: 'RS256' }],
      }),
    })
  })

  await page.route('**/oauth2/authorize**', async (route: Route) => {
    authorizeCount += 1
    const authorizationRequest = new URL(route.request().url())
    const redirectUri = authorizationRequest.searchParams.get('redirect_uri') ?? callbackUri
    const expectedState = authorizationRequest.searchParams.get('state') ?? ''
    receivedCodeChallenge = authorizationRequest.searchParams.get('code_challenge')
    receivedNonce = authorizationRequest.searchParams.get('nonce')

    const callback = new URL(redirectUri)
    if (callbackCase === 'cancelled') {
      callback.searchParams.set('error', 'access_denied')
      callback.searchParams.set('error_description', 'Sign-in was cancelled by the user.')
      callback.searchParams.set('state', expectedState)
    } else {
      if (callbackCase !== 'missing-code') callback.searchParams.set('code', callbackCase === 'invalid-code' ? 'invalid-code' : 'fixture-code')
      if (callbackCase !== 'missing-state') {
        callback.searchParams.set('state', callbackCase === 'invalid-state' ? 'unmatched-state' : expectedState)
      }
    }
    callbackUrl = callback.toString()

    await route.fulfill({ status: 302, headers: { location: callbackUrl } })
  })

  await page.route('**/oauth2/token', async (route: Route) => {
    tokenExchangeCount += 1
    const form = new URLSearchParams(route.request().postData() ?? '')
    receivedCodeVerifier = form.get('code_verifier')

    if (callbackCase === 'invalid-code' || form.get('code') !== 'fixture-code') {
      await route.fulfill({
        status: 400,
        headers: corsHeaders(),
        body: JSON.stringify({ error: 'invalid_grant', error_description: 'Invalid authorization code' }),
      })
      return
    }

    await route.fulfill({
      status: 200,
      headers: corsHeaders(),
      body: JSON.stringify({
        access_token: 'fixture-access-token',
        id_token: signIdToken(receivedNonce ?? ''),
        token_type: 'Bearer',
        expires_in: accessTokenLifetime,
        scope: 'openid email',
      }),
    })
  })

  await page.route(`${apiBaseUrl}/api/me`, async (route: Route) => {
    await route.fulfill({
      status: 200,
      headers: corsHeaders(),
      body: JSON.stringify({ subject: 'fixture-cognito-subject', roles: ['Controller'] }),
    })
  })

  return {
    get authorizeCount() { return authorizeCount },
    get tokenExchangeCount() { return tokenExchangeCount },
    get callbackUrl() { return callbackUrl },
    get receivedCodeVerifier() { return receivedCodeVerifier },
    get receivedCodeChallenge() { return receivedCodeChallenge },
  }
}

async function beginSignIn(page: Page) {
  await page.goto('/')
  await page.getByRole('button', { name: /sign in/i }).click()
}

async function expectControllerWorkspaceHidden(page: Page) {
  await expect(page.getByRole('heading', { name: /controller/i })).toHaveCount(0)
  await expect(page.getByText(/controller workspace/i)).toHaveCount(0)
}

test.describe('Controller managed-login browser protocol (mocked local fixture)', () => {
  test('valid state and code exchange establish the Controller shell exactly once and clean callback history', async ({ page }) => {
    const fixture = await installLocalOidcFixture(page, 'valid')
    await beginSignIn(page)

    await expect(page.getByRole('heading', { name: /controller/i })).toBeVisible()
    expect(fixture.authorizeCount).toBe(1)
    expect(fixture.tokenExchangeCount).toBe(1)
    expect(fixture.receivedCodeVerifier).toBeTruthy()
    expect(fixture.receivedCodeChallenge).toBe(
      createHash('sha256').update(fixture.receivedCodeVerifier!).digest('base64url'),
    )

    await expect.poll(() => page.evaluate(() => sessionStorage.length)).toBe(0)
    expect(await page.evaluate(() => localStorage.length)).toBe(0)
    expect(await page.evaluate(() => document.cookie)).toBe('')

    const landedUrl = new URL(page.url())
    expect(landedUrl.searchParams.has('code')).toBe(false)
    expect(landedUrl.searchParams.has('state')).toBe(false)
    await page.goBack()
    await page.goForward()
    expect(new URL(page.url()).searchParams.has('code')).toBe(false)
    expect(new URL(page.url()).searchParams.has('state')).toBe(false)
  })

  for (const callbackCase of ['missing-state', 'invalid-state', 'missing-code', 'invalid-code', 'cancelled'] as const) {
    test(`${callbackCase} callback fails closed and clears callback URL data`, async ({ page }) => {
      const fixture = await installLocalOidcFixture(page, callbackCase)
      await beginSignIn(page)

      await expect(page.getByRole('heading', { name: /sign-in could not be completed/i })).toBeVisible()
      await expectControllerWorkspaceHidden(page)
      expect(fixture.authorizeCount).toBe(1)
      if (callbackCase === 'invalid-code') expect(fixture.tokenExchangeCount).toBe(1)
      else expect(fixture.tokenExchangeCount).toBe(0)

      const visibleUrl = new URL(page.url())
      expect(visibleUrl.searchParams.has('code')).toBe(false)
      expect(visibleUrl.searchParams.has('state')).toBe(false)
      expect(visibleUrl.searchParams.has('error_description')).toBe(false)
    })
  }

  test('a consumed OAuth state cannot be replayed after a fresh page load', async ({ page }) => {
    const fixture = await installLocalOidcFixture(page, 'valid')
    await beginSignIn(page)
    await expect(page.getByRole('heading', { name: /controller/i })).toBeVisible()
    expect(fixture.callbackUrl).toBeTruthy()

    // Reload drops the volatile OIDC user. The already-consumed transaction must not restore it.
    await page.reload()
    await page.goto(fixture.callbackUrl!)
    await expect(page.getByRole('heading', { name: /sign-in could not be completed/i })).toBeVisible()
    await expectControllerWorkspaceHidden(page)
    expect(fixture.tokenExchangeCount).toBe(1)
    expect(new URL(page.url()).searchParams.has('code')).toBe(false)
    expect(new URL(page.url()).searchParams.has('state')).toBe(false)
  })

  test('access-token expiry closes the Controller shell and returns to the sign-in entry', async ({ page }) => {
    await installLocalOidcFixture(page, 'valid', 2)
    await beginSignIn(page)
    await expect(page.getByRole('heading', { name: /controller workspace/i })).toBeVisible()

    await expect(page.getByRole('link', { name: /sign in/i })).toBeVisible({ timeout: 10_000 })
    await expectControllerWorkspaceHidden(page)
  })
})
