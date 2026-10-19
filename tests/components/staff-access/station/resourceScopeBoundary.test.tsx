import { render, screen } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { describe, expect, it } from 'vitest'
import { IdentityContext, type IdentityState } from '../../../../src/features/staff-access/identity/identityContext'
import { stateForCurrentSession } from '../../../../src/features/staff-access/identity/identityState'
import { ProtectedRoute } from '../../../../src/features/staff-access/routes/ProtectedRoute'
import { StationStaffWorkspace } from '../../../../src/features/staff-access/workspaces/StationStaffWorkspace'

function renderStationStaffRoute(state: IdentityState, entry: string) {
  const router = createMemoryRouter(
    [
      {
        path: '/station-staff/*',
        element: (
          <ProtectedRoute requiredRole="Station Staff">
            <StationStaffWorkspace />
          </ProtectedRoute>
        ),
      },
    ],
    { initialEntries: [entry] },
  )

  return render(
    <IdentityContext.Provider value={{ state }}>
      <RouterProvider router={router} />
    </IdentityContext.Provider>,
  )
}

describe('Station Staff resource-scope boundary', () => {
  it('keeps the workspace generic when station and resource values are selected in the URL', () => {
    renderStationStaffRoute(
      {
        status: 'authenticated',
        identity: { subject: 'station-staff-1', roles: ['Station Staff'] },
      },
      '/station-staff?stationId=STA-02&resourceId=RESOURCE-14',
    )

    expect(screen.getByRole('heading', { name: /station staff workspace/i })).toBeTruthy()
    expect(screen.queryByText(/STA-02|RESOURCE-14/i)).toBeNull()
    expect(screen.queryByText(/assigned station|station assignment|resource access granted/i)).toBeNull()
  })

  it('does not let selected station or resource values bypass the returned role', () => {
    renderStationStaffRoute(
      {
        status: 'authenticated',
        identity: { subject: 'passenger-1', roles: ['Passenger'] },
      },
      '/station-staff/stations/STA-02/resources/RESOURCE-14',
    )

    expect(screen.queryByRole('heading', { name: /station staff workspace/i })).toBeNull()
    expect(screen.getByRole('alert').textContent).toMatch(/no access|not authorized|no permission/i)
    expect(screen.queryByText(/STA-02|RESOURCE-14/i)).toBeNull()
  })

  it('denies a Controller-only identity on the Station Staff route', () => {
    renderStationStaffRoute(
      {
        status: 'authenticated',
        identity: { subject: 'controller-1', roles: ['Controller'] },
      },
      '/station-staff',
    )

    expect(screen.queryByRole('heading', { name: /station staff workspace/i })).toBeNull()
    expect(screen.getByRole('alert').textContent).toMatch(/no access|not authorized|no permission/i)
  })

  it('keeps the protected shell hidden while identity is loading', () => {
    renderStationStaffRoute({ status: 'loading' }, '/station-staff')

    expect(screen.getByRole('status').textContent).toMatch(/loading/i)
    expect(screen.queryByRole('heading', { name: /station staff workspace/i })).toBeNull()
  })

  it('does not restore an old identity while a replacement session is loading', () => {
    const currentState = stateForCurrentSession(
      {
        accessToken: 'old-session-token',
        state: {
          status: 'authenticated',
          identity: { subject: 'station-staff-1', roles: ['Station Staff'] },
        },
      },
      'replacement-session-token',
    )
    renderStationStaffRoute(currentState, '/station-staff')

    expect(screen.getByRole('status').textContent).toMatch(/loading/i)
    expect(screen.queryByRole('heading', { name: /station staff workspace/i })).toBeNull()
  })

  it('removes Station Staff presentation when the current role set changes', () => {
    const router = createMemoryRouter(
      [
        {
          path: '/station-staff/*',
          element: (
            <ProtectedRoute requiredRole="Station Staff">
              <StationStaffWorkspace />
            </ProtectedRoute>
          ),
        },
      ],
      { initialEntries: ['/station-staff'] },
    )
    const renderWithState = (state: IdentityState) => (
      <IdentityContext.Provider value={{ state }}>
        <RouterProvider router={router} />
      </IdentityContext.Provider>
    )
    const view = render(renderWithState({
      status: 'authenticated',
      identity: { subject: 'station-staff-1', roles: ['Station Staff'] },
    }))

    expect(screen.getByRole('heading', { name: /station staff workspace/i })).toBeTruthy()
    view.rerender(renderWithState({
      status: 'authenticated',
      identity: { subject: 'station-staff-1', roles: ['Controller'] },
    }))

    expect(screen.queryByRole('heading', { name: /station staff workspace/i })).toBeNull()
    expect(screen.getByRole('alert').textContent).toMatch(/no access|not authorized|no permission/i)
  })
})
