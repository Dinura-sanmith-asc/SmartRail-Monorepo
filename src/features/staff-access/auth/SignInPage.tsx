import { useEffect, useMemo, useState } from 'react'
import { useAuth } from 'react-oidc-context'
import { Link, Navigate, useLocation, useNavigate } from 'react-router'
import { AccessState } from '../../../shared/ui/AccessState'
import { safeSameOriginReturnPath } from './oidcConfig'
import { useIdentity } from '../identity/identityContext'
import { IdentityRecovery } from '../identity/IdentityRecovery'
import { useLocalSession } from './localSessionContext'
import { clearTransactionState } from './sessionStore'
import { createSignOutHandler } from './signOut'
import { AuthPageLayout } from './AuthPageLayout'

export function SignInPage() {
  const auth = useAuth()
  const { state, retryIdentity } = useIdentity()
  const session = useLocalSession()
  const location = useLocation()
  const navigate = useNavigate()
  const [starting, setStarting] = useState(false)
  const [startFailed, setStartFailed] = useState(false)
  const [noticeDismissed, setNoticeDismissed] = useState(false)
  const { isAuthenticated, removeUser } = auth
  const logoutNotice = location.state && typeof location.state === 'object' && 'logoutNotice' in location.state
    && typeof location.state.logoutNotice === 'string'
    ? location.state.logoutNotice
    : null

  useEffect(() => {
    if (state.status === 'reauthentication-required' && isAuthenticated) void removeUser()
  }, [isAuthenticated, removeUser, state.status])

  const signOut = useMemo(() => createSignOutHandler({
    remoteLogoutUrl: session.remoteLogoutUrl,
    invalidateLocalSession: session.invalidateLocalSession,
    clearTransactionState,
    removeUser: session.removeUser,
    navigateToSignIn: () => navigate('/', { replace: true, state: { logoutNotice: 'You have signed out of SmartRail Operations.' } }),
    navigateToRemoteLogout: (url) => window.location.assign(url),
    onRemoteLogoutUnavailable: () => navigate('/', { replace: true, state: { logoutNotice: 'You are signed out locally. The provider sign-out page could not be opened.' } }),
  }), [navigate, session.invalidateLocalSession, session.remoteLogoutUrl, session.removeUser])

  if (state.status === 'authenticated' && state.identity.roles.includes('Controller')) return <Navigate to="/controller" replace />
  if (state.status === 'authenticated' && state.identity.roles.includes('Station Staff')) return <Navigate to="/station-staff" replace />
  if (state.status === 'authenticated' && state.identity.roles.includes('Driver')) return <Navigate to="/driver" replace />

  const returnTo = safeSameOriginReturnPath(
    `${location.pathname}${location.search}${location.hash}`,
    window.location.origin,
  )

  async function beginSignIn() {
    setStarting(true)
    setStartFailed(false)
    try {
      await auth.signinRedirect({ state: { returnTo } })
    } catch {
      setStarting(false)
      setStartFailed(true)
    }
  }

  async function leaveNoAccessSession() {
    setStarting(true)
    await signOut()
    setStarting(false)
  }

  if (state.status === 'loading') {
    return <AuthPageLayout page="Checking staff access">
      <h2 id="signin-heading">Checking your session</h2>
      <p className="auth-card__description">SmartRail is confirming your current staff access before opening a workspace.</p>
      <div className="auth-progress" role="status"><span className="auth-progress__spinner" aria-hidden="true" />Checking access with SmartRail…</div>
      <div className="auth-recovery-actions"><button className="auth-link-button" type="button" onClick={retryIdentity}>Check again</button></div>
    </AuthPageLayout>
  }

  if (state.status === 'authenticated' || state.status === 'no-operational-access') {
    return <AuthPageLayout page="Workspace access">
      <AccessState state="no-access" />
      <p className="auth-card__description">Your SmartRail identity is signed in, but it is not assigned to an operational workspace. Contact your administrator to review your access.</p>
      {session.available && <button className="auth-action auth-action--secondary" type="button" disabled={starting} onClick={() => void leaveNoAccessSession()}>{starting ? 'Signing out…' : 'Sign out and use another account'}<ArrowIcon /></button>}
      {!session.available && <Link className="auth-link" to="/">Return to sign in <ArrowIcon /></Link>}
    </AuthPageLayout>
  }

  return <AuthPageLayout page="STAFF ACCESS">
    <h2 id="signin-heading">Staff sign in</h2>
    <p className="auth-card__description">Continue with your organization’s managed sign-in to access SmartRail railway operations.</p>
    {!noticeDismissed && logoutNotice && <p className="auth-feedback auth-feedback--notice" role="status">{logoutNotice}<button className="auth-dismiss" type="button" aria-label="Dismiss notification" onClick={() => setNoticeDismissed(true)}>×</button></p>}
    {startFailed && <p className="auth-feedback" role="alert">Sign-in could not be started. Check your connection and try again.</p>}
    {auth.error && <p className="auth-feedback" role="alert">The previous sign-in attempt was not completed. You can safely try again.</p>}
    {(state.status === 'authority-unavailable' || state.status === 'error') && <IdentityRecovery state={state} />}
    {state.status === 'reauthentication-required' && <p className="auth-feedback auth-feedback--notice" role="status">Your session ended. Sign in again to continue.</p>}
    <div className="auth-roles" aria-label="Available staff workspaces"><span>Controller<small>Network operations</small></span><span>Station Staff<small>Station operations</small></span></div>
    <button className="auth-action" type="button" aria-label="Sign in" disabled={starting} onClick={() => void beginSignIn()}>
      {starting ? <><span className="auth-progress__spinner" aria-hidden="true" />Redirecting to secure sign in…</> : <>Continue with organization sign in <ArrowIcon /></>}
    </button>
    <div className="auth-card__hint"><ShieldIcon /><span>Authentication is handled by your organization’s secure Cognito managed login. SmartRail does not ask for your password here.</span></div>
  </AuthPageLayout>
}

function ArrowIcon() { return <svg aria-hidden="true" viewBox="0 0 20 20"><path d="M3.5 10h12m-5-5 5 5-5 5" /></svg> }
function ShieldIcon() { return <svg aria-hidden="true" viewBox="0 0 20 20"><path d="M10 2.5 16 5v4.7c0 3.7-2.4 6.3-6 7.8-3.6-1.5-6-4.1-6-7.8V5l6-2.5Z" /><path d="m7.2 9.9 1.8 1.8 3.8-4" /></svg> }
