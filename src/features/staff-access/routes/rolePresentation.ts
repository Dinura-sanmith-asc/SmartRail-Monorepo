import type { StaffRole } from '../identity/currentIdentity'

export type WorkspaceAffordance = {
  role: 'Controller' | 'Station Staff'
  label: string
  href: string
}

const workspaceAffordances: readonly WorkspaceAffordance[] = [
  { role: 'Controller', label: 'Controller Workspace', href: '/controller' },
  { role: 'Station Staff', label: 'Station Staff Workspace', href: '/station-staff' },
]

/** Return only navigation affordances supported by the current backend role set. */
export function getWorkspaceAffordances(roles: readonly string[]): WorkspaceAffordance[] {
  return workspaceAffordances.filter((affordance) => roles.includes(affordance.role))
}

export function hasWorkspaceRole(roles: readonly StaffRole[], requiredRole: StaffRole) {
  return (requiredRole === 'Controller' || requiredRole === 'Station Staff') && roles.includes(requiredRole)
}
