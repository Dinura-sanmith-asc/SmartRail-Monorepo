import type { UserManagerSettings } from 'oidc-client-ts'
import type { PublicRuntimeConfig } from '../../../config/runtimeConfig'
import { createTransactionStateStore, InMemoryStateStore } from './sessionStore'

export function createOidcConfig(config: PublicRuntimeConfig): UserManagerSettings {
  const { issuer, managedLoginDomain, appClientId, callbackUri, scopes } = config.cognito
  const managedLogin = new URL(managedLoginDomain)

  return {
    authority: issuer,
    client_id: appClientId,
    redirect_uri: callbackUri,
    response_type: 'code',
    scope: scopes.join(' '),
    automaticSilentRenew: false,
    revokeTokensOnSignout: false,
    disablePKCE: false,
    response_mode: 'query',
    userStore: new InMemoryStateStore(),
    stateStore: createTransactionStateStore(),
    metadataSeed: {
      issuer,
      authorization_endpoint: new URL('/oauth2/authorize', managedLogin).toString(),
      token_endpoint: new URL('/oauth2/token', managedLogin).toString(),
    },
  }
}

/** Resolves only same-origin in-app return targets; unsafe values fail back to /. */
export function safeSameOriginReturnPath(candidate: unknown, appOrigin: string): string {
  if (typeof candidate !== 'string' || !candidate || candidate.includes('\\')) return '/'

  try {
    const base = new URL(appOrigin)
    const target = new URL(candidate, base)
    if (target.origin !== base.origin || target.username || target.password || !target.pathname.startsWith('/') || target.pathname.startsWith('//')) {
      return '/'
    }
    return `${target.pathname}${target.search}${target.hash}`
  } catch {
    return '/'
  }
}
