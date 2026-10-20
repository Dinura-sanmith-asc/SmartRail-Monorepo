import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { http, HttpResponse } from 'msw'
import { describe, expect, it, vi } from 'vitest'
import { server } from '../../../src/test/msw'
import { IdentityContext } from '../../../src/features/staff-access/identity/identityContext'
import { DriverWorkspace } from '../../../src/features/driver/DriverWorkspace'

vi.mock('react-oidc-context', () => ({ useAuth: () => ({ user: { access_token: 'driver-test-token' } }) }))
vi.mock('../../../src/config/runtimeConfigContext', () => ({ useRuntimeConfig: () => ({ apiBaseUrl: 'https://api.test' }) }))

describe('driver workspace', () => {
  it('loads own assignments and submits a non-authoritative observation', async () => {
    const user = userEvent.setup()
    let submission: unknown
    server.use(http.get('https://api.test/api/driver/assignments', () => HttpResponse.json({ items: [{
      assignmentId: 'assignment-1', driverSubject: 'driver-1', journey: { snapshot: {
        journeyInstanceId: 'journey-1', serviceId: 'service-1', operationDate: '2026-10-20', revision: 3,
        status: 'Running', stops: [{ journeyStopId: 'stop-1', stopOrder: 1, stationId: 'station-1', stationName: 'Colombo Fort',
          plannedArrivalAt: null, plannedDepartureAt: '2026-10-20T04:00:00Z', operationalArrivalAt: null,
          operationalDepartureAt: null, platform: '2', ambiguity: 'none' }], source: 'Published', publishedAt: null,
        lastUpdatedAt: null, eta: null,
      }, updates: [] },
    }] })), http.post('https://api.test/api/driver/journeys/journey-1/observations', async ({ request }) => {
      submission = await request.json()
      return HttpResponse.json({ observationId: 'observation-1' }, { status: 201 })
    }))
    const router = createMemoryRouter([{ path: '/driver', element: <DriverWorkspace /> }], { initialEntries: ['/driver'] })
    render(<IdentityContext.Provider value={{ state: { status: 'authenticated', identity: { subject: 'driver-1', roles: ['Driver'] } } }}><RouterProvider router={router} /></IdentityContext.Provider>)

    expect(await screen.findByText('Colombo Fort')).toBeTruthy()
    await user.type(screen.getByLabelText('Report journey progress'), 'Arrived at the assigned stop')
    await user.click(screen.getByRole('button', { name: 'Submit observation' }))
    await waitFor(() => expect(submission).toMatchObject({ text: 'Arrived at the assigned stop' }))
    expect((submission as { requestId: string }).requestId).toMatch(/^[0-9a-f-]{36}$/i)
    expect((await screen.findByRole('status')).textContent).toMatch(/submitted to Control/i)
  })
})
