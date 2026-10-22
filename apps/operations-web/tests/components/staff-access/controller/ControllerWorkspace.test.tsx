import { render, screen } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { IdentityContext, type IdentityState } from '../../../../src/features/staff-access/identity/identityContext'
import { ProtectedRoute } from '../../../../src/features/staff-access/routes/ProtectedRoute'
import { ControllerWorkspace } from '../../../../src/features/staff-access/workspaces/ControllerWorkspace'
import type { StaffRole } from '../../../../src/features/staff-access/identity/currentIdentity'

vi.mock('react-oidc-context', () => ({ useAuth: () => ({ user: null }) }))
vi.mock('../../../../src/config/runtimeConfigContext', () => ({ useRuntimeConfig: () => ({ apiBaseUrl: 'https://api.test' }) }))

function renderControllerRoute(state: IdentityState) {
  const router = createMemoryRouter(
    [
      {
        path: '/controller/*',
        element: (
          <ProtectedRoute requiredRole="Controller">
            <ControllerWorkspace />
          </ProtectedRoute>
        ),
      },
    ],
    { initialEntries: ['/controller'] },
  )

  return render(
    <IdentityContext.Provider value={{ state }}>
      <RouterProvider router={router} />
    </IdentityContext.Provider>,
  )
}

describe('Controller protected route and workspace shell', () => {
  it('shows the Controller workspace for a current Controller role', () => {
    renderControllerRoute({
      status: 'authenticated',
      identity: { subject: 'controller-1', roles: ['Controller'] },
    })

    expect(screen.getByRole('heading', { name: /control room/i })).toBeTruthy()
  })

  it('denies a direct protected-route visit when no operational role is returned', () => {
    renderControllerRoute({ status: 'no-operational-access' })

    expect(screen.queryByRole('heading', { name: /control room/i })).toBeNull()
    expect(screen.getByRole('alert').textContent).toMatch(/no access|not authorized|no permission/i)
  })

  it.each(['Passenger', 'Driver'] as const)(
    'does not grant the Controller workspace to a %s-only identity',
    (role) => {
      renderControllerRoute({
        status: 'authenticated',
        identity: { subject: 'non-controller-1', roles: [role] },
      })

      expect(screen.queryByRole('heading', { name: /control room/i })).toBeNull()
      expect(screen.getByRole('alert').textContent).toMatch(/no access|not authorized|no permission/i)
    },
  )

  it('does not infer Controller access from an unexpected role label', () => {
    renderControllerRoute({
      status: 'authenticated',
      identity: {
        subject: 'unexpected-role-1',
        roles: ['Railway Superuser' as StaffRole],
      },
    })

    expect(screen.queryByRole('heading', { name: /control room/i })).toBeNull()
    expect(screen.getByRole('alert').textContent).toMatch(/no access|not authorized|no permission/i)
  })

  it('keeps a directly requested protected route closed while unauthenticated', () => {
    renderControllerRoute({ status: 'unauthenticated' })

    expect(screen.queryByRole('heading', { name: /control room/i })).toBeNull()
    expect(screen.getByRole('alert').textContent).toMatch(/sign in|authentication|required/i)
  })
})
