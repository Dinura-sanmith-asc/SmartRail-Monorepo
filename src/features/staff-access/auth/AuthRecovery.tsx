import { Link } from 'react-router'
import { AuthPageLayout } from './AuthPageLayout'

export function AuthRecovery() {
  return <AuthPageLayout page="Session recovery">
    <h2 id="auth-recovery-heading">Sign in to continue</h2>
    <p className="auth-card__description" role="alert">Your session is not available. Sign in to access this workspace.</p>
    <Link className="auth-action" to="/">Return to staff sign in <span aria-hidden="true">→</span></Link>
    <div className="auth-card__hint"><span aria-hidden="true">i</span><span>Your protected workspace stays closed until SmartRail verifies a current staff session.</span></div>
  </AuthPageLayout>
}
