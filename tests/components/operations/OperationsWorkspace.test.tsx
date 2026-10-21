import { render, screen, waitFor, within } from '@testing-library/react'
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

function renderStationStaff() {
  const router = createMemoryRouter([{ path: '/station-staff', element: <OperationsWorkspace role="Station Staff" /> }], { initialEntries: ['/station-staff'] })
  return render(<IdentityContext.Provider value={{ state: { status: 'authenticated', identity: { subject: 'staff-test', roles: ['Station Staff'] } } }}><RouterProvider router={router} /></IdentityContext.Provider>)
}

describe('operations workspace interactions', () => {
  it('creates a train service and publishes its timetable through the management APIs', async () => {
    const user = userEvent.setup()
    const serviceId = 'service-generated-by-api'
    const route = { routeId: 'route-1', routeCode: 'R-1', name: 'Central Line', isPublished: true, stops: [
      { routeStopId: 'stop-1', stationId: 'station-1', stationName: 'Central', stopOrder: 1 },
      { routeStopId: 'stop-2', stationId: 'station-2', stationName: 'Harbor', stopOrder: 2 },
    ] }
    let services: Record<string, unknown>[] = []
    let revisions: Record<string, unknown>[] = []
    let serviceBody: unknown
    let timetableBody: unknown
    const published: string[] = []
    server.use(
      http.get('https://api.test/api/stations', () => HttpResponse.json({ items: [] })),
      http.get('https://api.test/api/incidents', () => HttpResponse.json({ items: [] })),
      http.get('https://api.test/api/services/management', () => HttpResponse.json(services)),
      http.get('https://api.test/api/routes/management', () => HttpResponse.json([route])),
      http.get('https://api.test/api/services/:id/schedules', () => HttpResponse.json(revisions)),
      http.post('https://api.test/api/services', async ({ request }) => {
        serviceBody = await request.json()
        const created = { serviceId, serviceCode: 'SRL-TEST', displayName: 'Harbor Express', publicServiceNumber: '804', routeId: route.routeId, routeCode: route.routeCode, routeName: route.name, isPublished: false }
        services = [created]
        return HttpResponse.json(created, { status: 201 })
      }),
      http.post('https://api.test/api/services/:id/schedules', async ({ request }) => {
        timetableBody = await request.json()
        const created = { scheduleId: 'schedule-1', revision: 1, firstDepartureLocalTime: '06:30:00', isPublished: false, publishedAt: null, supersededAt: null, callingPoints: [
          { ...route.stops[0], arrivalOffsetMinutes: null, departureOffsetMinutes: 0 },
          { ...route.stops[1], arrivalOffsetMinutes: 45, departureOffsetMinutes: null },
        ], operationDates: [] }
        revisions = [created]
        return HttpResponse.json(created, { status: 201 })
      }),
      http.post('https://api.test/api/services/:id/schedules/:scheduleId/publish', ({ params }) => {
        published.push(`${String(params.id)}:${String(params.scheduleId)}`)
        return new HttpResponse(null, { status: 204 })
      }),
    )
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    renderController()
    await user.click(screen.getByRole('button', { name: /Train Services/i }))
    await user.click(await screen.findByRole('button', { name: 'Add Train Service' }))
    await user.type(screen.getByLabelText('Service code'), 'SRL-TEST')
    await user.selectOptions(screen.getByLabelText('Route'), route.routeId)
    await user.type(screen.getByLabelText('Display name'), 'Harbor Express')
    await user.type(screen.getByLabelText('Public service number'), '804')
    await user.click(screen.getByRole('button', { name: 'Add service' }))
    expect(await screen.findByText('Harbor Express')).toBeTruthy()
    expect(serviceBody).toEqual({ serviceCode: 'SRL-TEST', routeId: route.routeId, displayName: 'Harbor Express', publicServiceNumber: '804' })

    await user.click(screen.getByRole('button', { name: 'Create timetable revision' }))
    await user.clear(screen.getByLabelText('First departure (Asia/Colombo)'))
    await user.type(screen.getByLabelText('First departure (Asia/Colombo)'), '06:30')
    await user.click(screen.getByRole('button', { name: 'Save draft revision' }))
    expect(await screen.findByText('Revision 1')).toBeTruthy()
    expect(timetableBody).toMatchObject({ firstDepartureLocalTime: '06:30:00', callingPoints: [{ routeStopId: 'stop-1' }, { routeStopId: 'stop-2' }] })
    await user.click(screen.getByRole('button', { name: 'Publish' }))
    await waitFor(() => expect(published).toEqual([`${serviceId}:schedule-1`]))
  })

  it('supports station create, edit, delete, and refreshes the list after each change', async () => {
    const user = userEvent.setup()
    let stations = [{ stationId: 'station-1', name: 'Central' }]
    const requests: string[] = []
    server.use(
      http.get('https://api.test/api/stations', () => HttpResponse.json({ items: stations })),
      http.get('https://api.test/api/incidents', () => HttpResponse.json({ items: [] })),
      http.post('https://api.test/api/stations', async ({ request }) => {
        const body = await request.json() as { name: string }
        requests.push(`POST:${body.name}`)
        stations = [...stations, { stationId: 'station-new', name: body.name }]
        return HttpResponse.json({ stationId: 'station-new', name: body.name }, { status: 201 })
      }),
      http.patch('https://api.test/api/stations/:id', async ({ request, params }) => {
        const body = await request.json() as { name: string }
        requests.push(`PATCH:${String(params.id)}:${body.name}`)
        stations = stations.map((station) => station.stationId === params.id ? { ...station, name: body.name } : station)
        return HttpResponse.json({ stationId: String(params.id), name: body.name })
      }),
      http.delete('https://api.test/api/stations/:id', ({ params }) => {
        requests.push(`DELETE:${String(params.id)}`)
        stations = stations.filter((station) => station.stationId !== params.id)
        return new HttpResponse(null, { status: 204 })
      }),
    )
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    renderController()
    await user.click(screen.getByRole('button', { name: /^Stations/ }))
    await user.click(await screen.findByRole('button', { name: 'Add Station' }))
    await user.type(screen.getByLabelText('Station name'), 'Harbor')
    await user.click(screen.getByRole('button', { name: 'Add station' }))
    expect(await screen.findByText('Harbor')).toBeTruthy()
    await user.click(screen.getAllByRole('button', { name: 'Edit' }).at(-1)!)
    const nameField = screen.getByLabelText('Station name')
    await user.clear(nameField)
    await user.type(nameField, 'Harbor Central')
    await user.click(screen.getByRole('button', { name: 'Save changes' }))
    expect(await screen.findByText('Harbor Central')).toBeTruthy()
    await user.click(screen.getAllByRole('button', { name: 'Delete' }).at(-1)!)
    await waitFor(() => expect(requests).toEqual(['POST:Harbor', 'PATCH:station-new:Harbor Central', 'DELETE:station-new']))
    expect(screen.queryByText('Harbor Central')).toBeNull()
  })

  it('keeps the Controller overview isolated from the Station Staff workspace', async () => {
    server.use(
      http.get('https://api.test/api/stations', () => HttpResponse.json({ items: [{ stationId: 'station-1', name: 'Central' }] })),
      http.get('https://api.test/api/incidents', () => HttpResponse.json({ items: [] })),
      http.get('https://api.test/api/stations/station-1/departures', () => HttpResponse.json([])),
    )
    renderStationStaff()
    expect(await screen.findByRole('heading', { name: /station operations/i })).toBeTruthy()
    expect(screen.queryByRole('heading', { name: /operational overview and live journey status/i })).toBeNull()
    expect(screen.queryByRole('button', { name: /Scheduled Journeys/ })).toBeNull()
    expect(screen.getByRole('button', { name: /Station overview/i })).toBeTruthy()
  })

  it('opens existing Journey Control details from the global overview row', async () => {
    server.use(
      http.get('https://api.test/api/operations/overview', ({ request }) => {
        const operatingDate = new URL(request.url).searchParams.get('date') ?? '2026-10-20'
        return HttpResponse.json({
        operationalDate: operatingDate,
        metrics: { totalScheduled: 1, running: 1, delayed: 0, openIncidents: 0, cancelled: 0, completed: 0 },
        journeys: [{ journeyId: 'journey-global', serviceId: 'service-global', serviceCode: 'SR-CODE', serviceDisplayName: 'Service Name', publicServiceNumber: '82', routeCode: 'R', routeName: 'Route', operationDate: operatingDate, origin: 'Origin', destination: 'Destination', scheduledDeparture: null, scheduledArrival: null, status: 'Running', recordedDelayMinutes: null, lastReportedStation: null, lastUpdateAt: `${operatingDate}T04:00:00Z`, assignedDriver: null }],
      })}),
      http.get('https://api.test/api/stations', () => HttpResponse.json({ items: [] })),
      http.get('https://api.test/api/incidents', () => HttpResponse.json({ items: [] })),
      http.get('https://api.test/api/journeys/journey-global', () => HttpResponse.json({ journeyInstanceId: 'journey-global', serviceId: 'service-global', operationDate: '2026-10-20', revision: 1, status: 'Running', stops: [], source: 'ControllerManual', publishedAt: null, lastUpdatedAt: null, eta: null })),
      http.get('https://api.test/api/journeys/journey-global/updates', () => HttpResponse.json([])),
      http.get('https://api.test/api/operations/journeys/journey-global/observations', () => HttpResponse.json({ items: [] })),
    )
    renderController()
    const board = await screen.findByRole('table', { name: /Live journey board/ })
    await userEvent.setup().click(within(board).getByRole('button', { name: /View details/ }))
    expect(await screen.findByRole('heading', { name: 'journey-global' })).toBeTruthy()
    expect(screen.getByRole('button', { name: /Journey control/i })).toBeTruthy()
  })

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
    await user.click(within(screen.getByRole('navigation', { name: 'Workspace sections' })).getByRole('button', { name: /incident management/i }))
    await user.type(screen.getByLabelText('Approved category code'), 'APPROVED_TEST_CODE')
    await user.type(screen.getByLabelText('Incident description'), 'Track access issue near platform two')
    await user.click(screen.getByRole('button', { name: /submit incident report/i }))

    await waitFor(() => expect(submitted).toEqual({ categoryCode: 'APPROVED_TEST_CODE', description: 'Track access issue near platform two', associations: [{ kind: 'Station', resourceId: 'station-1' }] }))
    expect(await screen.findByText(/incident report submitted/i)).toBeTruthy()
    expect(idempotencyKey).toMatch(/^[0-9a-f-]{36}$/i)
  })
})
