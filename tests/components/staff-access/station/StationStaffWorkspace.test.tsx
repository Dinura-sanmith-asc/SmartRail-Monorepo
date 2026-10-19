import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { describe, expect, it } from 'vitest'
import { IdentityContext } from '../../../../src/features/staff-access/identity/identityContext'
import type { StaffRole } from '../../../../src/features/staff-access/identity/currentIdentity'
import { StationStaffWorkspace } from '../../../../src/features/staff-access/workspaces/StationStaffWorkspace'

function renderWorkspace(roles: StaffRole[]) {
  const router = createMemoryRouter([{ path: '/station-staff/*', element: <StationStaffWorkspace /> }], {
    initialEntries: ['/station-staff'],
  })
  return render(
    <IdentityContext.Provider
      value={{ state: { status: 'authenticated', identity: { subject: 'station-staff-1', roles } } }}
    >
      <RouterProvider router={router} />
    </IdentityContext.Provider>,
  )
}

describe('Station Staff workspace', () => {
  it('shows the generic Station Staff workspace and its approved navigation', () => {
    renderWorkspace(['Station Staff'])

    expect(screen.getByRole('heading', { name: 'Station Staff Workspace' })).toBeTruthy()
    expect(screen.getByRole('navigation', { name: /operations navigation/i })).toBeTruthy()
    expect(screen.getByRole('link', { name: /station staff workspace/i }).getAttribute('aria-current')).toBe('page')
    expect(screen.queryByRole('link', { name: /controller/i })).toBeNull()
  })

  it('shows the union of navigation affordances for both recognized staff roles', () => {
    renderWorkspace(['Station Staff', 'Controller'])

    expect(screen.getByRole('link', { name: /station staff workspace/i })).toBeTruthy()
    expect(screen.getByRole('link', { name: /controller workspace/i })).toBeTruthy()
  })

  it('keeps navigation keyboard-operable', async () => {
    const user = userEvent.setup()
    renderWorkspace(['Station Staff'])

    await user.tab()
    expect(document.activeElement).toBe(screen.getByRole('link', { name: /station staff workspace/i }))
    await user.keyboard('{Enter}')
    expect(screen.getByRole('link', { name: /station staff workspace/i }).getAttribute('aria-current')).toBe('page')
  })
})
