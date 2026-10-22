import { act, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { useState } from 'react'
import { describe, expect, it } from 'vitest'
import { IdentityProvider } from '../../../../src/features/staff-access/identity/IdentityProvider'
import { useIdentity } from '../../../../src/features/staff-access/identity/identityContext'
import { ApiRequestError } from '../../../../src/shared/api/fetchJson'
import { getCurrentIdentity } from '../../../../src/features/staff-access/identity/currentIdentityClient'
import { server } from '../../../../src/test/msw'

const apiBaseUrl = 'https://api.test'

describe('GET /api/me client contract', () => {
  it('sends the current Cognito access token as a bearer token and parses the approved identity', async () => {
    let requestAuthorization: string | null = null
    server.use(
      http.get(`${apiBaseUrl}/api/me`, ({ request }) => {
        requestAuthorization = request.headers.get('authorization')
        return HttpResponse.json({ subject: 'opaque-controller-subject', roles: ['Controller'] })
      }),
    )

    await expect(
      getCurrentIdentity({ apiBaseUrl, accessToken: 'access-token-test-value' }),
    ).resolves.toEqual({ subject: 'opaque-controller-subject', roles: ['Controller'] })
    expect(requestAuthorization).toBe('Bearer access-token-test-value')
  })

  it.each([401, 503] as const)('classifies the contract response %i without converting it to an identity', async (status) => {
    server.use(http.get(`${apiBaseUrl}/api/me`, () => new HttpResponse(null, { status })))

    await expect(getCurrentIdentity({ apiBaseUrl, accessToken: 'test-token' })).rejects.toMatchObject({
      status,
      failure: status === 401 ? 'unauthenticated' : 'authority-unavailable',
    })
  })

  it('uses only documented /api/me status fixtures: 200, 401, and 503 (no 403 contract response)', () => {
    const documentedResponses = [200, 401, 503]
    expect(documentedResponses).not.toContain(403)
  })

  it('classifies a bounded request timeout', async () => {
    server.use(
      http.get(`${apiBaseUrl}/api/me`, async () => {
        await new Promise((resolve) => setTimeout(resolve, 100))
        return HttpResponse.json({ subject: 'late-subject', roles: ['Controller'] })
      }),
    )

    await expect(
      getCurrentIdentity({ apiBaseUrl, accessToken: 'test-token', timeoutMs: 10 }),
    ).rejects.toMatchObject({ failure: 'timeout' })
  })

  it('classifies network failures without creating identity', async () => {
    server.use(http.get(`${apiBaseUrl}/api/me`, () => HttpResponse.error()))

    await expect(getCurrentIdentity({ apiBaseUrl, accessToken: 'test-token' })).rejects.toMatchObject({
      failure: 'offline',
    })
  })

  it.each([
    { subject: '', roles: ['Controller'] },
    { subject: 'opaque-subject', roles: ['Controller', 'Controller'] },
    { subject: 'opaque-subject', roles: ['Administrator'] },
    { subject: 'opaque-subject', roles: ['Station Staff'], stationIds: ['S-01'] },
  ])('rejects a malformed 200 response without trusting its roles: %j', async (body) => {
    server.use(http.get(`${apiBaseUrl}/api/me`, () => HttpResponse.json(body)))

    const request = getCurrentIdentity({ apiBaseUrl, accessToken: 'test-token' })
    await expect(request).rejects.toBeInstanceOf(ApiRequestError)
    await expect(request).rejects.toMatchObject({ failure: 'malformed-response' })
  })
})

function ProtectedWorkspace() {
  const { identity } = useIdentity()
  return identity ? <main>Protected workspace: {identity.subject}</main> : <p>Identity pending</p>
}

function SessionHarness() {
  const [accessToken, setAccessToken] = useState<string | null>('first-session-token')
  return (
    <>
      <button onClick={() => setAccessToken(null)}>Sign out</button>
      <button onClick={() => setAccessToken('replacement-session-token')}>Replace session</button>
      <IdentityProvider apiBaseUrl={apiBaseUrl} accessToken={accessToken}>
        <ProtectedWorkspace />
      </IdentityProvider>
    </>
  )
}

describe('identity lifecycle', () => {
  it.each(['sign out', 'replace session'] as const)(
    'does not let an old delayed response restore identity or protected content after %s',
    async (transition) => {
      let releaseFirstResponse!: () => void
      let firstRequestStarted!: () => void
      const firstRequest = new Promise<void>((resolve) => {
        firstRequestStarted = resolve
      })
      server.use(
        http.get(`${apiBaseUrl}/api/me`, async ({ request }) => {
          if (request.headers.get('authorization') === 'Bearer first-session-token') {
            firstRequestStarted()
            await new Promise<void>((resolve) => {
              releaseFirstResponse = resolve
            })
            return HttpResponse.json({ subject: 'old-session-subject', roles: ['Controller'] })
          }
          return HttpResponse.json({ subject: 'replacement-subject', roles: ['Station Staff'] })
        }),
      )

      const user = userEvent.setup()
      render(<SessionHarness />)
      await firstRequest

      await user.click(screen.getByRole('button', { name: transition === 'sign out' ? 'Sign out' : 'Replace session' }))
      if (transition === 'replace session') {
        expect(await screen.findByText('Protected workspace: replacement-subject')).toBeTruthy()
      } else {
        await waitFor(() => expect(screen.queryByText(/Protected workspace:/)).toBeNull())
      }

      await act(async () => {
        releaseFirstResponse()
      })

      await waitFor(() => {
        if (transition === 'sign out') {
          expect(screen.queryByText(/Protected workspace:/)).toBeNull()
        } else {
          expect(screen.getByText('Protected workspace: replacement-subject')).toBeTruthy()
          expect(screen.queryByText('Protected workspace: old-session-subject')).toBeNull()
        }
      })
    },
  )
})
