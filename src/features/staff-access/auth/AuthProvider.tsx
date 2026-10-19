import { useCallback, useEffect, useState, type ReactNode } from 'react'
import { AuthProvider as OidcAuthProvider, useAuth } from 'react-oidc-context'
import type { UserManagerSettings } from 'oidc-client-ts'
import { IdentityProvider } from '../identity/IdentityProvider'
import { clearOAuthResponseFromUrl } from './callbackUrl'
import { registerAccessTokenExpiry } from './sessionLifecycle'
import { LocalSessionContext } from './localSessionContext'

function IdentitySessionBridge({ apiBaseUrl, remoteLogoutUrl, children }: { apiBaseUrl: string; remoteLogoutUrl: string | null; children: ReactNode }) {
  const auth = useAuth()
  const { events, removeUser } = auth
  const [isTerminated, setIsTerminated] = useState(false)

  const invalidateLocalSession = useCallback(() => setIsTerminated(true), [])

  const clearLocalSession = useCallback(() => {
    invalidateLocalSession()
    void removeUser().catch(() => undefined)
  }, [invalidateLocalSession, removeUser])

  useEffect(() => registerAccessTokenExpiry(
    (onExpired) => events.addAccessTokenExpired(onExpired),
    clearLocalSession,
  ), [events, clearLocalSession])

  const accessToken = !isTerminated && auth.isAuthenticated ? auth.user?.access_token ?? null : null
  return (
    <LocalSessionContext.Provider value={{ available: true, isTerminated, invalidateLocalSession, remoteLogoutUrl, removeUser }}>
      <IdentityProvider
        apiBaseUrl={apiBaseUrl}
        accessToken={accessToken}
        onAuthenticationRequired={clearLocalSession}
      >
        {children}
      </IdentityProvider>
    </LocalSessionContext.Provider>
  )
}

export function StaffAuthProvider({
  settings,
  apiBaseUrl,
  remoteLogoutUrl,
  children,
}: {
  settings: UserManagerSettings
  apiBaseUrl: string
  remoteLogoutUrl: string | null
  children: ReactNode
}) {
  const handleSignin = useCallback(() => {
    clearOAuthResponseFromUrl()
  }, [])

  return (
    <OidcAuthProvider {...settings} onSigninCallback={handleSignin}>
      <IdentitySessionBridge apiBaseUrl={apiBaseUrl} remoteLogoutUrl={remoteLogoutUrl}>{children}</IdentitySessionBridge>
    </OidcAuthProvider>
  )
}
