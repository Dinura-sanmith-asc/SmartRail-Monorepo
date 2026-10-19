export type PublicRuntimeConfig = {
  apiBaseUrl: string
  cognito: {
    issuer: string
    managedLoginDomain: string
    appClientId: string
    callbackUri: string
    logoutUri: string
    scopes: string[]
  }
}

export class RuntimeConfigError extends Error {
  constructor(message: string) {
    super(message)
    this.name = 'RuntimeConfigError'
  }
}

type Environment = Record<string, unknown>

function requiredValue(environment: Environment, key: string): string {
  const value = environment[key]
  if (typeof value !== 'string' || !value.trim()) {
    throw new RuntimeConfigError(`Missing required public configuration: ${key}`)
  }
  return value.trim()
}

function absoluteUrl(environment: Environment, key: string, trimRootSlash = false): string {
  const value = requiredValue(environment, key)
  let parsed: URL
  try {
    parsed = new URL(value)
  } catch {
    throw new RuntimeConfigError(`Invalid URL in public configuration: ${key}`)
  }
  const localDevelopment = parsed.protocol === 'http:' && ['localhost', '127.0.0.1', '[::1]'].includes(parsed.hostname)
  if (parsed.protocol !== 'https:' && !localDevelopment) {
    throw new RuntimeConfigError(`Public configuration URL must use HTTPS: ${key}`)
  }
  return trimRootSlash && parsed.pathname === '/' && !parsed.search && !parsed.hash
    ? parsed.origin
    : parsed.toString()
}

export function parseRuntimeConfig(environment: Environment): PublicRuntimeConfig {
  if (Object.keys(environment).some((key) => /CLIENT_SECRET/i.test(key))) {
    throw new RuntimeConfigError('Client secrets are not allowed in public runtime configuration')
  }

  const scopes = requiredValue(environment, 'VITE_COGNITO_SCOPES').split(/\s+/).filter(Boolean)
  if (!scopes.includes('openid')) {
    throw new RuntimeConfigError('Cognito scopes must include openid')
  }

  return {
    apiBaseUrl: absoluteUrl(environment, 'VITE_SMARTRAIL_API_BASE_URL', true),
    cognito: {
      issuer: absoluteUrl(environment, 'VITE_COGNITO_ISSUER'),
      managedLoginDomain: absoluteUrl(environment, 'VITE_COGNITO_MANAGED_LOGIN_DOMAIN', true),
      appClientId: requiredValue(environment, 'VITE_COGNITO_APP_CLIENT_ID'),
      callbackUri: absoluteUrl(environment, 'VITE_COGNITO_CALLBACK_URI'),
      logoutUri: absoluteUrl(environment, 'VITE_COGNITO_LOGOUT_URI'),
      scopes: [...new Set(scopes)],
    },
  }
}
