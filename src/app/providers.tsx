import type { ReactNode } from 'react'
import { useMemo } from 'react'
import { parseRuntimeConfig, RuntimeConfigError } from '../config/runtimeConfig'
import { RuntimeConfigContext, useRuntimeConfig } from '../config/runtimeConfigContext'
import { createOidcConfig } from '../features/staff-access/auth/oidcConfig'
import { StaffAuthProvider } from '../features/staff-access/auth/AuthProvider'
import { buildCognitoLogoutUrl } from '../features/staff-access/auth/cognitoLogout'

export function RuntimeConfigProvider({ children }: { children: ReactNode }) {
  const result = useMemo(() => {
    try {
      return { config: parseRuntimeConfig(import.meta.env as Record<string, unknown>), error: null }
    } catch (error) {
      return {
        config: null,
        error: error instanceof RuntimeConfigError ? error.message : 'Application configuration is unavailable.',
      }
    }
  }, [])

  if (result.error) {
    return (
      <main className="startup-error" role="alert">
        <h1>SmartRail Operations Web is unavailable</h1>
        <p>{result.error}</p>
        <p>Contact your system administrator for assistance.</p>
      </main>
    )
  }

  return result.config ? <RuntimeConfigContext.Provider value={result.config}>{children}</RuntimeConfigContext.Provider> : null
}

export function ConfiguredAuthProvider({ children }: { children: ReactNode }) {
  const config = useRuntimeConfig()
  const setup = useMemo(() => {
    try {
      return { settings: createOidcConfig(config), unavailable: false }
    } catch {
      return { settings: null, unavailable: true }
    }
  }, [config])
  const remoteLogoutUrl = useMemo(() => {
    try {
      return buildCognitoLogoutUrl(config)
    } catch {
      return null
    }
  }, [config])
  if (setup.unavailable || !setup.settings) {
    return (
      <main role="alert">
        <h1>Staff sign-in is unavailable</h1>
        <p>This browser cannot start the secure sign-in flow. Contact your system administrator.</p>
      </main>
    )
  }
  return <StaffAuthProvider settings={setup.settings} apiBaseUrl={config.apiBaseUrl} remoteLogoutUrl={remoteLogoutUrl}>{children}</StaffAuthProvider>
}
