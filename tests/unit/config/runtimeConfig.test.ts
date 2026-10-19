import { describe, expect, it } from 'vitest'
import { parseRuntimeConfig, RuntimeConfigError } from '../../../src/config/runtimeConfig'

const validEnvironment = {
  VITE_SMARTRAIL_API_BASE_URL: 'https://api.example.test',
  VITE_COGNITO_ISSUER: 'https://cognito-idp.example.test/pool',
  VITE_COGNITO_MANAGED_LOGIN_DOMAIN: 'https://login.example.test',
  VITE_COGNITO_APP_CLIENT_ID: 'public-client-id',
  VITE_COGNITO_CALLBACK_URI: 'https://ops.example.test/auth/callback',
  VITE_COGNITO_LOGOUT_URI: 'https://ops.example.test/',
  VITE_COGNITO_SCOPES: 'openid email',
}

describe('parseRuntimeConfig', () => {
  it('returns validated public configuration when all required values are present', () => {
    expect(parseRuntimeConfig(validEnvironment)).toMatchObject({
      apiBaseUrl: 'https://api.example.test',
      cognito: {
        issuer: 'https://cognito-idp.example.test/pool',
        managedLoginDomain: 'https://login.example.test',
        appClientId: 'public-client-id',
        callbackUri: 'https://ops.example.test/auth/callback',
        logoutUri: 'https://ops.example.test/',
        scopes: ['openid', 'email'],
      },
    })
  })

  it('rejects a missing SmartRail API base URL', () => {
    const environment = { ...validEnvironment }
    delete (environment as Partial<typeof validEnvironment>).VITE_SMARTRAIL_API_BASE_URL
    expect(() => parseRuntimeConfig(environment)).toThrow(RuntimeConfigError)
  })

  it('rejects missing Cognito client and redirect configuration', () => {
    expect(() => parseRuntimeConfig({ VITE_SMARTRAIL_API_BASE_URL: validEnvironment.VITE_SMARTRAIL_API_BASE_URL })).toThrow(RuntimeConfigError)
  })

  it('allows the app to remain usable for local sign-out when remote logout is not configured', () => {
    const environment = { ...validEnvironment }
    delete (environment as Partial<typeof validEnvironment>).VITE_COGNITO_LOGOUT_URI
    expect(parseRuntimeConfig(environment).cognito.logoutUri).toBeNull()
  })

  it('rejects malformed endpoint and redirect URLs', () => {
    expect(() =>
      parseRuntimeConfig({
        ...validEnvironment,
        VITE_COGNITO_ISSUER: 'not-a-url',
      }),
    ).toThrow(RuntimeConfigError)
  })

  it('rejects a client-secret environment key without including its value in errors', () => {
    const secret = 'must-not-be-exposed'
    expect(() =>
      parseRuntimeConfig({
        ...validEnvironment,
        VITE_COGNITO_CLIENT_SECRET: secret,
      }),
    ).toThrow(RuntimeConfigError)
    try {
      parseRuntimeConfig({ ...validEnvironment, VITE_COGNITO_CLIENT_SECRET: secret })
    } catch (error) {
      expect(String(error)).not.toContain(secret)
    }
  })
})
