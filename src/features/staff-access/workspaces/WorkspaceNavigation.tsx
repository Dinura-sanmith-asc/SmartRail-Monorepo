import { useMemo, useState } from 'react'
import { NavLink, useNavigate } from 'react-router'
import { useLocalSession } from '../auth/localSessionContext'
import { clearTransactionState } from '../auth/sessionStore'
import { createSignOutHandler } from '../auth/signOut'
import { useIdentity } from '../identity/identityContext'
import { getWorkspaceAffordances } from '../routes/rolePresentation'
import './WorkspaceNavigation.css'

const remoteLogoutUnavailableMessage = 'You are signed out of SmartRail Operations, but the provider sign-out page could not be opened.'
const remoteLogoutStartedMessage = 'Your SmartRail Operations session is closed. Cognito sign-out is being attempted in this browser.'

export function WorkspaceNavigation() {
  const { identity } = useIdentity()
  const session = useLocalSession()
  const navigate = useNavigate()
  const [signingOut, setSigningOut] = useState(false)
  const affordances = getWorkspaceAffordances(identity?.roles ?? [])

  const signOut = useMemo(() => createSignOutHandler({
    remoteLogoutUrl: session.remoteLogoutUrl,
    invalidateLocalSession: session.invalidateLocalSession,
    clearTransactionState,
    removeUser: session.removeUser,
    navigateToSignIn: () => navigate('/', { replace: true, state: { logoutNotice: remoteLogoutStartedMessage } }),
    navigateToRemoteLogout: (url) => window.location.assign(url),
    onRemoteLogoutUnavailable: () => navigate('/', {
      replace: true,
      state: { logoutNotice: remoteLogoutUnavailableMessage },
    }),
  }), [navigate, session.invalidateLocalSession, session.remoteLogoutUrl, session.removeUser])

  async function handleSignOut() {
    setSigningOut(true)
    await signOut()
    setSigningOut(false)
  }

  return (
    <>
      <nav className="workspace-navigation" aria-label="Operations navigation">
        {affordances.map(({ role, label, href }) => (
          <NavLink key={role} to={href} end className="workspace-navigation__link">
            {label}
          </NavLink>
        ))}
      </nav>
      {session.available && (
        <>
          <button className="workspace-signout" type="button" onClick={() => void handleSignOut()} disabled={signingOut}>
            {signingOut ? 'Signing out…' : 'Sign out'}
          </button>
          {signingOut && <p role="status">Closing your SmartRail Operations session…</p>}
        </>
      )}
    </>
  )
}
