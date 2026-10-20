import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { readFileSync } from 'node:fs'
import { join } from 'node:path'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { AccessState } from '../../../../src/shared/ui/AccessState'
import { SignInPage } from '../../../../src/features/staff-access/auth/SignInPage'
import { IdentityContext, type IdentityState } from '../../../../src/features/staff-access/identity/identityContext'
import { ControllerWorkspace } from '../../../../src/features/staff-access/workspaces/ControllerWorkspace'
import { StationStaffWorkspace } from '../../../../src/features/staff-access/workspaces/StationStaffWorkspace'

const stationStaffCss = readFileSync(
  join(process.cwd(), 'src/features/staff-access/workspaces/StationStaffWorkspace.css'),
  'utf8',
)

const auth = vi.hoisted(() => ({
  signinRedirect: vi.fn(),
  removeUser: vi.fn(),
}))

vi.mock('react-oidc-context', () => ({ useAuth: () => ({ ...auth, isAuthenticated: false, error: null }) }))
vi.mock('../../../../src/config/runtimeConfigContext', () => ({ useRuntimeConfig: () => ({ apiBaseUrl: 'https://api.test' }) }))

function renderSignIn(state: IdentityState = { status: 'unauthenticated' }) {
  const router = createMemoryRouter([
    { path: '/', element: <SignInPage /> },
    { path: '/station-staff/*', element: <StationStaffWorkspace /> },
    { path: '/controller/*', element: <ControllerWorkspace /> },
  ], { initialEntries: ['/'] })
  return render(
    <IdentityContext.Provider value={{ state }}>
      <RouterProvider router={router} />
    </IdentityContext.Provider>,
  )
}

function renderStationWorkspace() {
  const router = createMemoryRouter([{ path: '/station-staff/*', element: <StationStaffWorkspace /> }], {
    initialEntries: ['/station-staff'],
  })
  return render(
    <IdentityContext.Provider
      value={{ state: { status: 'authenticated', identity: { subject: 'staff-1', roles: ['Station Staff'] } } }}
    >
      <RouterProvider router={router} />
    </IdentityContext.Provider>,
  )
}

describe('staff access recovery accessibility', () => {
  beforeEach(() => {
    auth.signinRedirect.mockReset().mockResolvedValue(undefined)
    auth.removeUser.mockReset().mockResolvedValue(undefined)
  })

  it('allows keyboard-only sign-in and gives the control an accessible name', async () => {
    const user = userEvent.setup()
    renderSignIn()

    const signIn = screen.getByRole('button', { name: 'Sign in' })
    await user.tab()
    expect(document.activeElement).toBe(signIn)
    await user.keyboard('{Enter}')

    expect(auth.signinRedirect).toHaveBeenCalledOnce()
  })

  it('announces sign-in failure and lets a keyboard user retry', async () => {
    const user = userEvent.setup()
    auth.signinRedirect.mockRejectedValueOnce(new Error('private provider detail'))
    renderSignIn()

    await user.click(screen.getByRole('button', { name: 'Sign in' }))
    const alert = await screen.findByRole('alert')
    expect(alert.textContent).toMatch(/sign-in could not be started|try again/i)
    expect(alert.textContent).not.toContain('private provider detail')

    const retry = screen.getByRole('button', { name: 'Sign in' })
    retry.blur()
    await user.tab()
    expect(document.activeElement).toBe(retry)
    await user.keyboard('{Enter}')
    expect(auth.signinRedirect).toHaveBeenCalledTimes(2)
  })

  it('announces recovery status through a live status region with text meaning', () => {
    render(<AccessState state="unavailable" onRetry={vi.fn()} />)

    const status = screen.getByRole('status')
    expect(status.closest('[aria-live]')?.getAttribute('aria-live')).toBe('polite')
    expect(status.textContent).toMatch(/temporarily unavailable|access information.*unavailable/i)
    expect(screen.getByRole('button', { name: /try again/i })).toBeTruthy()
  })

  it('keeps keyboard navigation named, focusable, and visibly focused', async () => {
    const user = userEvent.setup()
    renderStationWorkspace()

    const navigation = screen.getByRole('navigation', { name: 'Operations navigation' })
    const link = screen.getByRole('link', { name: 'Station Staff Workspace' })
    await user.tab()
    expect(document.activeElement).toBe(screen.getByRole('link', { name: /skip to operations content/i }))
    await user.tab()
    expect(document.activeElement).toBe(screen.getByRole('button', { name: /station overview/i }))
    await user.tab()
    expect(document.activeElement).toBe(screen.getByRole('button', { name: /incident reporting/i }))
    expect(navigation.contains(link)).toBe(true)
    expect(link.className).toContain('workspace-navigation__link')
    expect(stationStaffCss).toMatch(/\.workspace-navigation__link:focus-visible\s*\{[^}]*outline:\s*[^;]+/s)
    await user.keyboard('{Enter}')
    expect(link.getAttribute('aria-current')).toBe('page')
  })

  it('communicates denial in text without relying on a color cue', () => {
    render(<AccessState state="forbidden" />)

    const alert = screen.getByRole('alert')
    expect(alert.textContent).toMatch(/access denied|not authorized|no permission/i)
    expect(alert.textContent?.trim()).not.toBe('')
  })

  it('routes Station Staff to its workspace without Controller navigation', () => {
    renderSignIn({
      status: 'authenticated',
      identity: { subject: 'station-staff-1', roles: ['Station Staff'] },
    })

    expect(screen.getByRole('heading', { name: /station operations/i })).toBeTruthy()
    expect(screen.queryByRole('link', { name: /controller workspace/i })).toBeNull()
  })
})
