import { useEffect } from 'react'
import { useAuth } from 'react-oidc-context'
import { Link, useNavigate } from 'react-router'
import { safeSameOriginReturnPath } from './oidcConfig'
import { clearOAuthResponseFromUrl } from './callbackUrl'
import { AuthPageLayout } from './AuthPageLayout'

function returnPath(value: unknown): string {
  return safeSameOriginReturnPath(value, window.location.origin)
}

export function CallbackPage() {
  const auth = useAuth()
  const navigate = useNavigate()
  const failed = !auth.isLoading && (Boolean(auth.error) || !auth.isAuthenticated)

  useEffect(() => {
    if (auth.isLoading) return

    if (auth.error || !auth.isAuthenticated) {
      clearOAuthResponseFromUrl()
      return
    }

    if (auth.isAuthenticated && auth.user) {
      const userState = auth.user.state
      const path = userState && typeof userState === 'object' && 'returnTo' in userState
        ? returnPath(userState.returnTo)
        : '/'
      navigate(path, { replace: true })
      return
    }

  }, [auth.error, auth.isAuthenticated, auth.isLoading, auth.user, navigate])

  if (failed) {
    return (
      <AuthPageLayout page="Sign-in recovery">
        <h2 id="callback-heading">Sign-in could not be completed</h2>
        <p className="auth-card__description" role="alert">Your sign-in response could not be verified. No operational workspace was opened.</p>
        <Link className="auth-action" to="/">Return to staff sign in <span aria-hidden="true">→</span></Link>
      </AuthPageLayout>
    )
  }

  return <AuthPageLayout page="Secure sign in">
    <h2 id="callback-heading">Verifying your session</h2>
    <p className="auth-card__description">Completing the secure sign-in handoff and checking your staff access.</p>
    <div className="auth-progress" role="status"><span className="auth-progress__spinner" aria-hidden="true" />Verifying sign-in response…</div>
  </AuthPageLayout>
}
