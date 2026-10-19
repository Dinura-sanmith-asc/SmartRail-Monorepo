import { describe, expect, it } from 'vitest'
import { buildCognitoLogoutUrl } from '../../../../src/features/staff-access/auth/cognitoLogout'
import type { PublicRuntimeConfig } from '../../../../src/config/runtimeConfig'

const runtimeConfig: PublicRuntimeConfig = {
  apiBaseUrl: 'https://api.example.test',
  cognito: {
    issuer: 'https://issuer.example.test/pool',
    managedLoginDomain: 'https://login.example.test',
    appClientId: 'public-spa-client',
    callbackUri: 'https://ops.example.test/auth/callback',
    logoutUri: 'https://ops.example.test/signed-out?source=logout&next=%2F',
    scopes: ['openid'],
  },
}

describe('Cognito managed-login logout URL', () => {
  it('uses HTTPS managed-login /logout with the configured client and registered logout URI', () => {
    const logoutUrl = new URL(buildCognitoLogoutUrl(runtimeConfig))

    expect(logoutUrl.protocol).toBe('https:')
    expect(logoutUrl.origin).toBe(runtimeConfig.cognito.managedLoginDomain)
    expect(logoutUrl.pathname).toBe('/logout')
    expect([...logoutUrl.searchParams.keys()].sort()).toEqual(['client_id', 'logout_uri'])
    expect(logoutUrl.searchParams.get('client_id')).toBe(runtimeConfig.cognito.appClientId)
    expect(logoutUrl.searchParams.get('logout_uri')).toBe(runtimeConfig.cognito.logoutUri)
    expect(logoutUrl.search).toContain(encodeURIComponent(runtimeConfig.cognito.logoutUri))
    expect(logoutUrl.searchParams.has('post_logout_redirect_uri')).toBe(false)
    expect(logoutUrl.searchParams.has('access_token')).toBe(false)
    expect(logoutUrl.searchParams.has('id_token_hint')).toBe(false)
  })

  it.each([
    ['insecure managed-login domain', { ...runtimeConfig, cognito: { ...runtimeConfig.cognito, managedLoginDomain: 'http://login.example.test' } }],
    ['managed-login domain with credentials', { ...runtimeConfig, cognito: { ...runtimeConfig.cognito, managedLoginDomain: 'https://user:password@login.example.test' } }],
    ['insecure logout URI', { ...runtimeConfig, cognito: { ...runtimeConfig.cognito, logoutUri: 'http://ops.example.test/signed-out' } }],
    ['logout URI with credentials', { ...runtimeConfig, cognito: { ...runtimeConfig.cognito, logoutUri: 'https://user:password@ops.example.test/signed-out' } }],
    ['missing client ID', { ...runtimeConfig, cognito: { ...runtimeConfig.cognito, appClientId: ' ' } }],
    ['missing logout URI', { ...runtimeConfig, cognito: { ...runtimeConfig.cognito, logoutUri: null } }],
  ])('rejects %s', (_case, invalidConfig) => {
    expect(() => buildCognitoLogoutUrl(invalidConfig as PublicRuntimeConfig)).toThrow()
  })
})
