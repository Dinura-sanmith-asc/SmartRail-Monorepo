import { useEffect, useMemo, useRef, useState, type KeyboardEvent } from 'react'
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
  const [confirmingSignOut, setConfirmingSignOut] = useState(false)
  const signOutButtonRef = useRef<HTMLButtonElement>(null)
  const dialogWasOpen = useRef(false)
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
    setConfirmingSignOut(false)
    await signOut()
    setSigningOut(false)
  }

  useEffect(() => {
    if (confirmingSignOut) {
      dialogWasOpen.current = true
      return
    }
    if (dialogWasOpen.current) {
      dialogWasOpen.current = false
      signOutButtonRef.current?.focus()
    }
  }, [confirmingSignOut])

  function handleDialogKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key === 'Escape' && !signingOut) setConfirmingSignOut(false)
    if (event.key === 'Tab') {
      const buttons = event.currentTarget.querySelectorAll<HTMLButtonElement>('button:not(:disabled)')
      if (!buttons.length) return
      const first = buttons[0]
      const last = buttons[buttons.length - 1]
      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault()
        last.focus()
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault()
        first.focus()
      }
    }
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
          <button ref={signOutButtonRef} className="workspace-signout" type="button" onClick={() => setConfirmingSignOut(true)} disabled={signingOut}>
            {signingOut ? 'Signing out…' : 'Sign out'}
          </button>
          {signingOut && <p role="status">Closing your SmartRail Operations session…</p>}
          {confirmingSignOut && <div className="auth-dialog-backdrop" onKeyDown={handleDialogKeyDown}>
            <section className="auth-dialog" role="dialog" aria-modal="true" aria-labelledby="signout-dialog-heading" aria-describedby="signout-dialog-description">
              <span className="auth-card__eyebrow">END YOUR SESSION</span>
              <h2 id="signout-dialog-heading">Sign out of SmartRail?</h2>
              <p id="signout-dialog-description">Your local operations session will close, then SmartRail will attempt to sign you out through Cognito.</p>
              <div className="auth-dialog__actions"><button type="button" autoFocus onClick={() => setConfirmingSignOut(false)}>Stay signed in</button><button type="button" onClick={() => void handleSignOut()}>Sign out</button></div>
            </section>
          </div>}
        </>
      )}
    </>
  )
}
