import { OidcClient, WebStorageStateStore } from 'oidc-client-ts'
import { beforeEach, describe, expect, it } from 'vitest'
import { createOidcConfig, safeSameOriginReturnPath } from '../../../../src/features/staff-access/auth/oidcConfig'
import { InMemoryStateStore } from '../../../../src/features/staff-access/auth/sessionStore'
import type { PublicRuntimeConfig } from '../../../../src/config/runtimeConfig'

const runtimeConfig: PublicRuntimeConfig = {
  apiBaseUrl: 'https://api.example.test',
  cognito: {
    issuer: 'https://issuer.example.test/pool',
    managedLoginDomain: 'https://login.example.test',
    appClientId: 'public-spa-client',
    callbackUri: 'https://ops.example.test/auth/callback',
    logoutUri: 'https://ops.example.test/',
    scopes: ['openid', 'email'],
  },
}

describe('staff access OIDC configuration', () => {
  beforeEach(() => {
    window.sessionStorage.clear()
    window.localStorage.clear()
  })

  it('builds an Authorization Code configuration with PKCE S256 and no silent renewal or token revocation', async () => {
    const settings = createOidcConfig(runtimeConfig)

    expect(settings).toMatchObject({
      authority: runtimeConfig.cognito.issuer,
      client_id: runtimeConfig.cognito.appClientId,
      redirect_uri: runtimeConfig.cognito.callbackUri,
      response_type: 'code',
      automaticSilentRenew: false,
      revokeTokensOnSignout: false,
    })
    expect(settings.scope?.split(/\s+/)).toEqual(['openid', 'email'])
    expect(settings.disablePKCE).not.toBe(true)
    expect(settings.metadataSeed).toMatchObject({
      authorization_endpoint: `${runtimeConfig.cognito.managedLoginDomain}/oauth2/authorize`,
      token_endpoint: `${runtimeConfig.cognito.managedLoginDomain}/oauth2/token`,
    })

    // Seed metadata locally so the real protocol library creates its request without contacting Cognito.
    const client = new OidcClient({ ...settings, metadata: settings.metadataSeed })
    const request = await client.createSigninRequest({})
    const authorizationUrl = new URL(request.url)

    expect(authorizationUrl.origin).toBe(runtimeConfig.cognito.managedLoginDomain)
    expect(authorizationUrl.pathname).toBe('/oauth2/authorize')
    expect(authorizationUrl.searchParams.get('response_type')).toBe('code')
    expect(authorizationUrl.searchParams.get('client_id')).toBe(runtimeConfig.cognito.appClientId)
    expect(authorizationUrl.searchParams.get('redirect_uri')).toBe(runtimeConfig.cognito.callbackUri)
    expect(authorizationUrl.searchParams.get('code_challenge_method')).toBe('S256')
    expect(authorizationUrl.searchParams.get('code_challenge')).toMatch(/^[A-Za-z0-9_-]{43}$/)
    expect(authorizationUrl.searchParams.get('state')).toBeTruthy()

    // The redirect transaction, including the PKCE verifier, is per-tab and temporary.
    expect(settings.stateStore).toBeInstanceOf(WebStorageStateStore)
    expect(settings.userStore).toBeInstanceOf(InMemoryStateStore)
    expect('client_secret' in settings).toBe(false)
    const transactionEntries = Object.keys(window.sessionStorage)
    const transactionValues = Array.from({ length: window.sessionStorage.length }, (_, index) =>
      window.sessionStorage.getItem(window.sessionStorage.key(index) ?? '') ?? '',
    )
    expect(transactionEntries.length).toBeGreaterThan(0)
    expect([...transactionEntries, ...transactionValues].join(' ')).not.toMatch(/access_token|id_token|refresh_token/i)
    expect(window.localStorage.length).toBe(0)

    const userStore = settings.userStore as InMemoryStateStore
    const fixtureUser = JSON.stringify({ access_token: 'test-only-memory-token', id_token: 'test-only-id-token' })
    await userStore.set('oidc.user', fixtureUser)
    expect(await userStore.get('oidc.user')).toBe(fixtureUser)
    expect(window.localStorage.length).toBe(0)
    expect(window.sessionStorage.length).toBeGreaterThan(0)
    expect(Array.from({ length: window.sessionStorage.length }, (_, index) =>
      window.sessionStorage.getItem(window.sessionStorage.key(index) ?? '') ?? '',
    ).join(' ')).not.toContain('test-only-memory-token')
  })

  it.each([
    ['/controller/overview', '/controller/overview'],
    ['https://evil.example/path', '/'],
    ['//evil.example/path', '/'],
    ['\\\\evil.example\\path', '/'],
    ['/\\evil.example/path', '/'],
    ['javascript:alert(1)', '/'],
    ['', '/'],
    [null, '/'],
  ])('keeps the return target %s same-origin', (candidate, expected) => {
    expect(safeSameOriginReturnPath(candidate, 'https://ops.example.test')).toBe(expected)
  })

  it('accepts a same-origin absolute URL but returns only its in-app path and query', () => {
    expect(
      safeSameOriginReturnPath(
        'https://ops.example.test/station-staff?tab=alerts#overview',
        'https://ops.example.test',
      ),
    ).toBe('/station-staff?tab=alerts#overview')
  })
})
