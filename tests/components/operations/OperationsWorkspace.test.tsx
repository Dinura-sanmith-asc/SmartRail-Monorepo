import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { http, HttpResponse } from 'msw'
import { describe, expect, it, vi } from 'vitest'
import { server } from '../../../src/test/msw'
import { IdentityContext } from '../../../src/features/staff-access/identity/identityContext'
import { OperationsWorkspace } from '../../../src/features/operations/OperationsWorkspace'

vi.mock('react-oidc-context', () => ({ useAuth: () => ({ user: { access_token: 'operations-test-token' } }) }))
vi.mock('../../../src/config/runtimeConfigContext', () => ({ useRuntimeConfig: () => ({ apiBaseUrl: 'https://api.test' }) }))

function renderController() {
  const router = createMemoryRouter([{ path: '/controller', element: <OperationsWorkspace role="Controller" /> }], { initialEntries: ['/controller'] })
  return render(<IdentityContext.Provider value={{ state: { status: 'authenticated', identity: { subject: 'controller-test', roles: ['Controller'] } } }}><RouterProvider router={router} /></IdentityContext.Provider>)
}

describe('operations workspace interactions', () => {
  it('filters the departure board by operational status using fetched records', async () => {
    const user = userEvent.setup()
    let departureReads = 0
    server.use(
      http.get('https://api.test/api/stations', () => HttpResponse.json({ items: [{ stationId: 'station-1', name: 'Central' }] })),
      http.get('https://api.test/api/incidents', () => HttpResponse.json({ items: [] })),
      http.get('https://api.test/api/stations/station-1/departures', () => { departureReads += 1; return HttpResponse.json([
        { journeyInstanceId: 'journey-running', serviceId: 'service-1', operationDate: '2026-10-20', stationLocalDepartureDate: '2026-10-20', revision: 1, status: 'Running', stationId: 'station-1', stationName: 'Central', stopOrder: 1, scheduledDepartureAt: '2026-10-20T10:00:00+05:30', operationalDepartureAt: null, platform: '2', source: 'ScheduledBaseline', updatedAt: '2026-10-20T04:00:00Z', ambiguity: 'none' },
        { journeyInstanceId: 'journey-delayed', serviceId: 'service-2', operationDate: '2026-10-20', stationLocalDepartureDate: '2026-10-20', revision: 2, status: 'Delayed', stationId: 'station-1', stationName: 'Central', stopOrder: 1, scheduledDepartureAt: '2026-10-20T11:00:00+05:30', operationalDepartureAt: null, platform: null, source: 'ScheduledBaseline', updatedAt: '2026-10-20T04:00:00Z', ambiguity: 'none' },
      ]) }),
    )
    renderController()
    await waitFor(() => expect(departureReads).toBeGreaterThan(0))
    await screen.findByText('service-1')
    await user.selectOptions(screen.getByLabelText('Operational status'), 'Delayed')
    expect(screen.queryByText('service-1')).toBeNull()
    expect(screen.getByText('service-2')).toBeTruthy()
  })

  it('submits a validated station incident with the API contract and announces success', async () => {
    const user = userEvent.setup()
    let submitted: unknown
    let idempotencyKey = ''
    server.use(
      http.get('https://api.test/api/stations', () => HttpResponse.json({ items: [{ stationId: 'station-1', name: 'Central' }] })),
      http.get('https://api.test/api/incidents', () => HttpResponse.json({ items: [] })),
      http.get('https://api.test/api/stations/station-1/departures', () => HttpResponse.json([])),
      http.post('https://api.test/api/incidents', async ({ request }) => {
        submitted = await request.json()
        idempotencyKey = request.headers.get('Idempotency-Key') ?? ''
        return HttpResponse.json({ incidentId: 'incident-1' }, { status: 201 })
      }),
    )
    renderController()
    await user.click(await screen.findByRole('button', { name: /incident management/i }))
    await user.type(screen.getByLabelText('Approved category code'), 'APPROVED_TEST_CODE')
    await user.type(screen.getByLabelText('Incident description'), 'Track access issue near platform two')
    await user.click(screen.getByRole('button', { name: /submit incident report/i }))

    await waitFor(() => expect(submitted).toEqual({ categoryCode: 'APPROVED_TEST_CODE', description: 'Track access issue near platform two', associations: [{ kind: 'Station', resourceId: 'station-1' }] }))
    expect(await screen.findByText(/incident report submitted/i)).toBeTruthy()
    expect(idempotencyKey).toMatch(/^[0-9a-f-]{36}$/i)
  })
})
