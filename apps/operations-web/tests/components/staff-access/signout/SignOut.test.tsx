import { describe, expect, it, vi } from 'vitest'
import { createSignOutHandler } from '../../../../src/features/staff-access/auth/signOut'

function createSignOutOptions(overrides: Partial<Parameters<typeof createSignOutHandler>[0]> = {}) {
  const events: string[] = []
  const deps = {
    remoteLogoutUrl: 'https://login.example.test/logout?client_id=web&logout_uri=https%3A%2F%2Fweb.example.test%2F',
    invalidateLocalSession: vi.fn(() => { events.push('invalidate-local-session') }),
    clearTransactionState: vi.fn(() => { events.push('clear-transaction') }),
    removeUser: vi.fn(async () => { events.push('remove-user') }),
    navigateToSignIn: vi.fn(() => { events.push('navigate-sign-in') }),
    navigateToRemoteLogout: vi.fn(async () => { events.push('navigate-remote-logout') }),
    onRemoteLogoutUnavailable: vi.fn(() => { events.push('remote-logout-unavailable') }),
    ...overrides,
  }
  return { deps, events }
}

describe('local-first sign-out', () => {
  it('clears in-memory identity/token, protected cache and route, and active transaction before navigation', async () => {
    const state: {
      identity: { subject: string; roles: string[] } | null
      token: string | null
      protectedCache: string[]
      protectedRoute: string | null
      activeTransaction: string | null
    } = {
      identity: { subject: 'staff-1', roles: ['Controller'] },
      token: 'access-token',
      protectedCache: ['protected response'],
      protectedRoute: '/controller',
      activeTransaction: 'pkce-state',
    }
    const events: string[] = []
    const handler = createSignOutHandler({
      remoteLogoutUrl: 'https://login.example.test/logout?client_id=web&logout_uri=https%3A%2F%2Fweb.example.test%2F',
      invalidateLocalSession: () => {
        events.push('invalidate-local-session')
        state.identity = null
        state.token = null
        state.protectedCache = []
        state.protectedRoute = null
      },
      clearTransactionState: () => {
        events.push('clear-transaction')
        state.activeTransaction = null
      },
      removeUser: async () => { events.push('remove-user') },
      navigateToSignIn: () => { events.push('navigate-sign-in') },
      navigateToRemoteLogout: async () => { events.push('navigate-remote-logout') },
      onRemoteLogoutUnavailable: () => { events.push('remote-logout-unavailable') },
    })
    await handler()

    expect(state).toEqual({ identity: null, token: null, protectedCache: [], protectedRoute: null, activeTransaction: null })
    expect(events.indexOf('invalidate-local-session')).toBeLessThan(events.indexOf('navigate-sign-in'))
    expect(events.indexOf('clear-transaction')).toBeLessThan(events.indexOf('navigate-sign-in'))
    expect(events.indexOf('invalidate-local-session')).toBeLessThan(events.indexOf('navigate-remote-logout'))
    expect(events.indexOf('clear-transaction')).toBeLessThan(events.indexOf('navigate-remote-logout'))
  })

  it('still attempts configured remote logout if OIDC user removal rejects after local invalidation', async () => {
    const { deps, events } = createSignOutOptions({
      removeUser: vi.fn(async () => { events.push('remove-user'); throw new Error('OIDC store detail') }),
    })
    const handler = createSignOutHandler(deps)

    await expect(handler()).resolves.toBeUndefined()

    expect(events.indexOf('invalidate-local-session')).toBeLessThan(events.indexOf('navigate-remote-logout'))
    expect(events.indexOf('clear-transaction')).toBeLessThan(events.indexOf('navigate-remote-logout'))
    expect(deps.navigateToRemoteLogout).toHaveBeenCalledOnce()
  })

  it('falls back to the signed-out entry if remote logout navigation fails', async () => {
    const { deps, events } = createSignOutOptions({
      navigateToRemoteLogout: vi.fn(async () => { events.push('navigate-remote-logout'); throw new Error('navigation detail') }),
    })
    const handler = createSignOutHandler(deps)

    await expect(handler()).resolves.toBeUndefined()

    expect(events.indexOf('invalidate-local-session')).toBeLessThan(events.indexOf('navigate-remote-logout'))
    expect(events.indexOf('clear-transaction')).toBeLessThan(events.indexOf('navigate-remote-logout'))
    expect(deps.onRemoteLogoutUnavailable).toHaveBeenCalledOnce()
    expect(deps.navigateToSignIn).toHaveBeenCalledOnce()
  })

  it('coalesces in-flight and completed repeated calls without repeating navigation', async () => {
    let finishRemoteNavigation!: () => void
    const remoteNavigation = new Promise<void>((resolve) => { finishRemoteNavigation = resolve })
    const { deps } = createSignOutOptions({ navigateToRemoteLogout: vi.fn(() => remoteNavigation) })
    const handler = createSignOutHandler(deps)

    const first = handler()
    const concurrent = handler()
    await vi.waitFor(() => expect(deps.navigateToRemoteLogout).toHaveBeenCalledOnce())
    finishRemoteNavigation()
    await Promise.all([first, concurrent])
    await handler()

    expect(deps.invalidateLocalSession).toHaveBeenCalledOnce()
    expect(deps.clearTransactionState).toHaveBeenCalledOnce()
    expect(deps.navigateToRemoteLogout).toHaveBeenCalledOnce()
  })

  it('uses local fallback feedback when no remote logout URL is configured', async () => {
    const { deps, events } = createSignOutOptions({ remoteLogoutUrl: null })
    const handler = createSignOutHandler(deps)

    await expect(handler()).resolves.toBeUndefined()

    expect(deps.onRemoteLogoutUnavailable).toHaveBeenCalledOnce()
    expect(deps.navigateToSignIn).toHaveBeenCalledOnce()
    expect(deps.navigateToRemoteLogout).not.toHaveBeenCalled()
    expect(events.indexOf('invalidate-local-session')).toBeLessThan(events.indexOf('navigate-sign-in'))
    expect(events.indexOf('clear-transaction')).toBeLessThan(events.indexOf('navigate-sign-in'))
  })
})
