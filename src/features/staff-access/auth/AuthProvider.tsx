import { useCallback, useEffect, type ReactNode } from 'react'
import { AuthProvider as OidcAuthProvider, useAuth } from 'react-oidc-context'
import type { UserManagerSettings } from 'oidc-client-ts'
import { IdentityProvider } from '../identity/IdentityProvider'
import { clearOAuthResponseFromUrl } from './callbackUrl'

function IdentitySessionBridge({ apiBaseUrl, children }: { apiBaseUrl: string; children: ReactNode }) {
  const auth = useAuth()
  const { events, removeUser } = auth

  useEffect(() => events.addAccessTokenExpired(() => {
    void removeUser()
  }), [events, removeUser])

  const accessToken = auth.isAuthenticated ? auth.user?.access_token ?? null : null
  return <IdentityProvider apiBaseUrl={apiBaseUrl} accessToken={accessToken}>{children}</IdentityProvider>
}

export function StaffAuthProvider({
  settings,
  apiBaseUrl,
  children,
}: {
  settings: UserManagerSettings
  apiBaseUrl: string
  children: ReactNode
}) {
  const handleSignin = useCallback(() => {
    clearOAuthResponseFromUrl()
  }, [])

  return (
    <OidcAuthProvider {...settings} onSigninCallback={handleSignin}>
      <IdentitySessionBridge apiBaseUrl={apiBaseUrl}>{children}</IdentitySessionBridge>
    </OidcAuthProvider>
  )
}
