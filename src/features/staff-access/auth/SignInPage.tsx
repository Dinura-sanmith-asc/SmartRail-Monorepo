import { useEffect, useState } from 'react'
import { useAuth } from 'react-oidc-context'
import { Navigate, useLocation } from 'react-router'
import { AccessState } from '../../../shared/ui/AccessState'
import { safeSameOriginReturnPath } from './oidcConfig'
import { useIdentity } from '../identity/identityContext'
import { IdentityRecovery } from '../identity/IdentityRecovery'

export function SignInPage() {
  const auth = useAuth()
  const { state } = useIdentity()
  const location = useLocation()
  const [starting, setStarting] = useState(false)
  const [startFailed, setStartFailed] = useState(false)
  const { isAuthenticated, removeUser } = auth
  const logoutNotice = location.state && typeof location.state === 'object' && 'logoutNotice' in location.state
    && typeof location.state.logoutNotice === 'string'
    ? location.state.logoutNotice
    : null

  useEffect(() => {
    if (state.status === 'reauthentication-required' && isAuthenticated) {
      void removeUser()
    }
  }, [isAuthenticated, removeUser, state.status])

  if (state.status === 'loading') return <AccessState state="loading" />
  if (state.status === 'authenticated' && state.identity.roles.includes('Controller')) {
    return <Navigate to="/controller" replace />
  }
  if (state.status === 'authenticated' && state.identity.roles.includes('Station Staff')) {
    return <Navigate to="/station-staff" replace />
  }
  if (state.status === 'authenticated' || state.status === 'no-operational-access') {
    return (
      <main className="app-shell">
        <h1>SmartRail Operations</h1>
        <AccessState state="no-access" />
        <p>Your current SmartRail identity has no operational workspace access.</p>
      </main>
    )
  }

  const returnTo = safeSameOriginReturnPath(
    `${location.pathname}${location.search}${location.hash}`,
    window.location.origin,
  )

  async function signIn() {
    setStarting(true)
    setStartFailed(false)
    try {
      await auth.signinRedirect({ state: { returnTo } })
    } catch {
      setStarting(false)
      setStartFailed(true)
    }
  }

  return (
    <main className="app-shell" aria-labelledby="signin-heading">
      <p className="app-shell__eyebrow">SmartRail Operations</p>
      <h1 id="signin-heading">Staff sign in</h1>
      <p>Use your organization’s approved sign-in to access railway operations.</p>
      {logoutNotice && <p role="status">{logoutNotice}</p>}
      {startFailed && <p role="alert">Sign-in could not be started. Try again or contact your administrator.</p>}
      {(state.status === 'authority-unavailable' || state.status === 'error') && <IdentityRecovery state={state} />}
      {state.status === 'reauthentication-required' && <p role="status">Your session ended. Sign in again to continue.</p>}
      <button type="button" disabled={starting} onClick={() => void signIn()}>
        {starting ? 'Redirecting to sign in…' : 'Sign in'}
      </button>
      {auth.error && <p role="alert">The previous sign-in attempt was not completed.</p>}
    </main>
  )
}
