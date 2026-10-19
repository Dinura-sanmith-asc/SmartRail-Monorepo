import { useEffect } from 'react'
import { useAuth } from 'react-oidc-context'
import { Link, useNavigate } from 'react-router'
import { AccessState } from '../../../shared/ui/AccessState'
import { safeSameOriginReturnPath } from './oidcConfig'
import { clearOAuthResponseFromUrl } from './callbackUrl'

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
      <main className="app-shell" aria-labelledby="callback-heading">
        <h1 id="callback-heading">Sign-in could not be completed</h1>
        <p role="alert">Your sign-in response could not be verified. No operational workspace was opened.</p>
        <Link to="/">Return to sign in</Link>
      </main>
    )
  }

  return <AccessState state="loading" />
}
