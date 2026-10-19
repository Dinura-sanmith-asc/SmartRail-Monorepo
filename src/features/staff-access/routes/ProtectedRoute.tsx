import type { ReactNode } from 'react'
import { AccessState } from '../../../shared/ui/AccessState'
import { AuthRecovery } from '../auth/AuthRecovery'
import { IdentityRecovery } from '../identity/IdentityRecovery'
import { useIdentity } from '../identity/identityContext'
import type { StaffRole } from '../identity/currentIdentity'
import { hasWorkspaceRole } from './rolePresentation'
import { useLocalSession } from '../auth/localSessionContext'

export function ProtectedRoute({ requiredRole, children }: { requiredRole: StaffRole; children: ReactNode }) {
  const { state } = useIdentity()
  const { isTerminated } = useLocalSession()

  if (isTerminated) return <AuthRecovery />
  if (state.status === 'loading') return <AccessState state="loading" />
  if (state.status === 'unauthenticated' || state.status === 'reauthentication-required') return <AuthRecovery />
  if (state.status === 'authority-unavailable' || state.status === 'error') return <IdentityRecovery state={state} />
  if (state.status === 'no-operational-access') return <AccessState state="no-access" />
  if (!hasWorkspaceRole(state.identity.roles, requiredRole)) return <AccessState state="no-access" />

  return children
}
