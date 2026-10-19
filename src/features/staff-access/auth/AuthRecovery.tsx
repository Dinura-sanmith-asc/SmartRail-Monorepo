import { Link } from 'react-router'

export function AuthRecovery() {
  return (
    <main className="app-shell" aria-labelledby="auth-recovery-heading">
      <h1 id="auth-recovery-heading">Sign in to continue</h1>
      <p role="alert">Your session is not available. Sign in to access this workspace.</p>
      <Link to="/">Sign in</Link>
    </main>
  )
}
