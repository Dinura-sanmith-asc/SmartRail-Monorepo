import { act, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { useState } from 'react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { expect, it } from 'vitest'
import { IdentityProvider } from '../../../../src/features/staff-access/identity/IdentityProvider'
import { useIdentity } from '../../../../src/features/staff-access/identity/identityContext'
import { ProtectedRoute } from '../../../../src/features/staff-access/routes/ProtectedRoute'
import { server } from '../../../../src/test/msw'

const apiBaseUrl = 'https://api.test'

function ProtectedContent() {
  const { identity } = useIdentity()
  return <main>Protected operations for {identity?.subject}</main>
}

it('does not restore workspace access when an old /api/me response resolves after sign-out during identity loading', async () => {
  let releaseResponse!: () => void
  let requestStarted!: () => void
  const pending = new Promise<void>((resolve) => { releaseResponse = resolve })
  const started = new Promise<void>((resolve) => { requestStarted = resolve })
  const router = createMemoryRouter([
    {
      path: '/controller/*',
      element: <ProtectedRoute requiredRole="Controller"><ProtectedContent /></ProtectedRoute>,
    },
  ], { initialEntries: ['/controller'] })

  function SignOutDuringIdentityLoading() {
    const [accessToken, setAccessToken] = useState<string | null>('old-session-token')
    return (
      <>
        <button type="button" onClick={() => setAccessToken(null)}>Sign out</button>
        <IdentityProvider apiBaseUrl={apiBaseUrl} accessToken={accessToken}>
          <RouterProvider router={router} />
        </IdentityProvider>
      </>
    )
  }

  server.use(http.get(`${apiBaseUrl}/api/me`, async () => {
    requestStarted()
    await pending
    return HttpResponse.json({ subject: 'old-session-user', roles: ['Controller'] })
  }))
  const user = userEvent.setup()
  render(<SignOutDuringIdentityLoading />)
  await started
  expect(await screen.findByRole('heading', { name: /checking access/i })).toBeTruthy()

  await user.click(screen.getByRole('button', { name: /sign out/i }))
  expect(await screen.findByRole('heading', { name: /sign in to continue/i })).toBeTruthy()
  await act(async () => { releaseResponse() })

  expect(screen.queryByText(/protected operations/i)).toBeNull()
  expect(screen.getByRole('link', { name: /sign in/i })).toBeTruthy()
})
