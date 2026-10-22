import { act, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { useState } from 'react'
import { CallbackPage } from '../../../../src/features/staff-access/auth/CallbackPage'
import { IdentityProvider } from '../../../../src/features/staff-access/identity/IdentityProvider'
import { useIdentity } from '../../../../src/features/staff-access/identity/identityContext'
import { ProtectedRoute } from '../../../../src/features/staff-access/routes/ProtectedRoute'
import { AccessState } from '../../../../src/shared/ui/AccessState'
import { server } from '../../../../src/test/msw'

const callbackAuth = vi.hoisted(() => ({
  error: null as Error | null,
  isLoading: false,
  isAuthenticated: false,
  user: null as { state?: unknown } | null,
}))

vi.mock('react-oidc-context', () => ({ useAuth: () => callbackAuth }))

const apiBaseUrl = 'https://api.test'

function ProtectedContent() {
  const { identity } = useIdentity()
  return <main>Protected operations for {identity?.subject}</main>
}

function renderProtected(
  accessToken: string | null = 'test-access-token',
  timeoutMs?: number,
  onAuthenticationRequired?: () => void,
) {
  const router = createMemoryRouter(
    [
      {
        path: '/controller/*',
        element: (
          <ProtectedRoute requiredRole="Controller">
            <ProtectedContent />
          </ProtectedRoute>
        ),
      },
    ],
    { initialEntries: ['/controller'] },
  )

  return render(
    <IdentityProvider
      apiBaseUrl={apiBaseUrl}
      accessToken={accessToken}
      timeoutMs={timeoutMs}
      onAuthenticationRequired={onAuthenticationRequired}
    >
      <RouterProvider router={router} />
    </IdentityProvider>,
  )
}

describe('access recovery integration', () => {
  beforeEach(() => {
    callbackAuth.error = null
    callbackAuth.isLoading = false
    callbackAuth.isAuthenticated = false
    callbackAuth.user = null
  })

  it('keeps an unauthenticated direct route closed and provides a sign-in entry', async () => {
    renderProtected(null)

    expect(await screen.findByRole('heading', { name: /sign in to continue/i })).toBeTruthy()
    expect(screen.getByRole('link', { name: /sign in/i })).toBeTruthy()
    expect(screen.queryByText(/protected operations/i)).toBeNull()
  })

  it('clears the protected route after 401 and does not replay the identity request', async () => {
    let requestCount = 0
    const onAuthenticationRequired = vi.fn()
    server.use(
      http.get(`${apiBaseUrl}/api/me`, () => {
        requestCount += 1
        return new HttpResponse(null, { status: 401 })
      }),
    )

    renderProtected('test-access-token', undefined, onAuthenticationRequired)

    expect(await screen.findByRole('heading', { name: /sign in to continue/i })).toBeTruthy()
    expect(screen.queryByText(/protected operations/i)).toBeNull()
    expect(requestCount).toBe(1)
    expect(onAuthenticationRequired).toHaveBeenCalledOnce()
  })

  it('distinguishes 503 from an empty role list and retries only after user activation', async () => {
    let requestCount = 0
    const authorizationHeaders: string[] = []
    server.use(
      http.get(`${apiBaseUrl}/api/me`, ({ request }) => {
        requestCount += 1
        authorizationHeaders.push(request.headers.get('authorization') ?? '')
        return requestCount <= 2
          ? new HttpResponse(null, { status: 503 })
          : HttpResponse.json({ subject: 'controller-1', roles: ['Controller'] })
      }),
    )
    const user = userEvent.setup()
    renderProtected()

    expect(await screen.findByRole('heading', { name: /access information unavailable/i })).toBeTruthy()
    expect(screen.getByRole('status').textContent).toMatch(/temporarily unavailable/i)
    expect(screen.queryByText(/protected operations/i)).toBeNull()
    expect(requestCount).toBe(1)

    await user.click(screen.getByRole('button', { name: /try again/i }))
    expect(await screen.findByRole('heading', { name: /access information unavailable/i })).toBeTruthy()
    expect(screen.queryByText(/protected operations/i)).toBeNull()
    expect(requestCount).toBe(2)

    await user.click(screen.getByRole('button', { name: /try again/i }))
    expect(await screen.findByText('Protected operations for controller-1')).toBeTruthy()
    expect(requestCount).toBe(3)
    expect(authorizationHeaders).toEqual([
      'Bearer test-access-token',
      'Bearer test-access-token',
      'Bearer test-access-token',
    ])
  })

  it('shows no-operational-access for a successful identity with an empty role list', async () => {
    server.use(http.get(`${apiBaseUrl}/api/me`, () => HttpResponse.json({ subject: 'unassigned-1', roles: [] })))
    renderProtected()

    expect((await screen.findByRole('alert')).textContent).toMatch(/no access|not authorized|no permission/i)
    expect(screen.queryByText(/protected operations/i)).toBeNull()
  })

  it('fails closed on a malformed successful identity without retaining old access', async () => {
    server.use(http.get(`${apiBaseUrl}/api/me`, () => HttpResponse.json({ subject: 'bad-1', roles: ['Unknown'] })))
    renderProtected()

    expect(await screen.findByRole('heading', { name: /unexpected service response/i })).toBeTruthy()
    expect(screen.queryByText(/protected operations/i)).toBeNull()
  })

  it('shows safe generic forbidden feedback without treating it as a /api/me response', () => {
    render(<AccessState state="forbidden" />)

    expect(screen.getByRole('heading', { name: /access denied/i })).toBeTruthy()
    expect(screen.getByRole('alert').textContent).toMatch(/permission/i)
    expect(screen.queryByText(/station|resource|controller operation/i)).toBeNull()
  })

  it.each([
    ['offline', () => HttpResponse.error(), /network unavailable/i, /network connection/i],
    ['service failure', () => new HttpResponse(null, { status: 500 }), /service problem/i, /request could not be completed/i],
  ] as const)('shows safe %s feedback with an explicit retry', async (_name, response, heading, message) => {
    server.use(http.get(`${apiBaseUrl}/api/me`, () => response()))
    renderProtected()

    expect(await screen.findByRole('heading', { name: heading })).toBeTruthy()
    expect(screen.getByRole('alert').textContent).toMatch(message)
    expect(screen.getByRole('button', { name: /try again/i })).toBeTruthy()
    expect(screen.queryByText(/protected operations/i)).toBeNull()
  })

  it('shows a bounded timeout state instead of loading indefinitely', async () => {
    server.use(
      http.get(`${apiBaseUrl}/api/me`, async () => {
        await new Promise((resolve) => setTimeout(resolve, 100))
        return HttpResponse.json({ subject: 'late-1', roles: ['Controller'] })
      }),
    )
    renderProtected('test-access-token', 10)

    expect(await screen.findByRole('heading', { name: /request timed out/i })).toBeTruthy()
    expect(screen.queryByText(/protected operations/i)).toBeNull()
  })

  it('does not expose protected content while current identity is loading', async () => {
    let releaseResponse!: () => void
    const pending = new Promise<void>((resolve) => { releaseResponse = resolve })
    server.use(
      http.get(`${apiBaseUrl}/api/me`, async () => {
        await pending
        return HttpResponse.json({ subject: 'controller-1', roles: ['Controller'] })
      }),
    )
    const view = renderProtected()

    expect(await screen.findByRole('heading', { name: /checking access/i })).toBeTruthy()
    expect(screen.queryByText(/protected operations/i)).toBeNull()
    releaseResponse()
    expect(await screen.findByText('Protected operations for controller-1')).toBeTruthy()
    view.unmount()
  })

  it('does not let a delayed identity response restore access after session expiry', async () => {
    let releaseResponse!: () => void
    let requestStarted!: () => void
    const pending = new Promise<void>((resolve) => { releaseResponse = resolve })
    const started = new Promise<void>((resolve) => { requestStarted = resolve })
    const router = createMemoryRouter(
      [
        {
          path: '/controller/*',
          element: (
            <ProtectedRoute requiredRole="Controller">
              <ProtectedContent />
            </ProtectedRoute>
          ),
        },
      ],
      { initialEntries: ['/controller'] },
    )

    function ExpiringProtectedRoute() {
      const [accessToken, setAccessToken] = useState<string | null>('expiring-token')
      return (
        <>
          <button type="button" onClick={() => setAccessToken(null)}>Expire session</button>
          <IdentityProvider apiBaseUrl={apiBaseUrl} accessToken={accessToken}>
            <RouterProvider router={router} />
          </IdentityProvider>
        </>
      )
    }

    server.use(
      http.get(`${apiBaseUrl}/api/me`, async () => {
        requestStarted()
        await pending
        return HttpResponse.json({ subject: 'expired-session', roles: ['Controller'] })
      }),
    )
    const user = userEvent.setup()
    render(<ExpiringProtectedRoute />)
    await started

    await user.click(screen.getByRole('button', { name: /expire session/i }))
    expect(await screen.findByRole('heading', { name: /sign in to continue/i })).toBeTruthy()
    await act(async () => { releaseResponse() })

    await waitFor(() => expect(screen.queryByText(/protected operations/i)).toBeNull())
    expect(screen.getByRole('link', { name: /sign in/i })).toBeTruthy()
  })

  it('keeps callback cancellation failed and offers a return to sign-in link', async () => {
    callbackAuth.error = new Error('provider internals must not be displayed')
    const router = createMemoryRouter([{ path: '/auth/callback', element: <CallbackPage /> }], {
      initialEntries: ['/auth/callback?error=access_denied&state=untrusted'],
    })
    render(<RouterProvider router={router} />)

    expect(await screen.findByRole('heading', { name: /sign-in could not be completed/i })).toBeTruthy()
    expect(screen.getByRole('alert').textContent).not.toContain('provider internals')
    expect(screen.getByRole('link', { name: /return to staff sign in/i })).toBeTruthy()
    await waitFor(() => expect(window.location.search).not.toContain('state'))
  })
})
