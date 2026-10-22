import type { PublicRuntimeConfig } from '../../../config/runtimeConfig'

/** Build the Cognito managed-login logout URL from the configured public values. */
export function buildCognitoLogoutUrl(config: PublicRuntimeConfig): string {
  const clientId = config.cognito.appClientId.trim()
  if (!clientId || /\s/.test(clientId)) throw new Error('Cognito app-client ID is unavailable.')

  const domain = new URL(config.cognito.managedLoginDomain)
  if (!config.cognito.logoutUri) throw new Error('Cognito logout URI is unavailable.')
  const logoutUri = new URL(config.cognito.logoutUri)
  const localDevelopment = ['localhost', '127.0.0.1', '[::1]'].includes(logoutUri.hostname)
  if (domain.protocol !== 'https:' || domain.username || domain.password || domain.search || domain.hash || domain.pathname !== '/') {
    throw new Error('Cognito managed-login domain is invalid.')
  }
  if ((logoutUri.protocol !== 'https:' && !(localDevelopment && logoutUri.protocol === 'http:')) || logoutUri.username || logoutUri.password) {
    throw new Error('Cognito logout URI is invalid.')
  }

  const endpoint = new URL('/logout', domain.origin)
  endpoint.searchParams.set('client_id', clientId)
  endpoint.searchParams.set('logout_uri', logoutUri.toString())
  return endpoint.toString()
}
