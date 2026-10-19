import type { ReactNode } from 'react'
import { AccessState } from '../../../shared/ui/AccessState'
import { useIdentity } from '../identity/identityContext'
import type { StaffRole } from '../identity/currentIdentity'
import { hasWorkspaceRole } from './rolePresentation'

export function ProtectedRoute({ requiredRole, children }: { requiredRole: StaffRole; children: ReactNode }) {
  const { state } = useIdentity()

  if (state.status === 'loading') return <AccessState state="loading" />
  if (state.status === 'unauthenticated' || state.status === 'reauthentication-required') {
    return <div role="alert">Authentication is required. Sign in to continue.</div>
  }
  if (state.status === 'authority-unavailable') return <AccessState state="unavailable" />
  if (state.status === 'error') return <AccessState state={state.failure === 'offline' ? 'offline' : 'unavailable'} />
  if (state.status === 'no-operational-access') return <AccessState state="no-access" />
  if (!hasWorkspaceRole(state.identity.roles, requiredRole)) return <AccessState state="no-access" />

  return children
}
