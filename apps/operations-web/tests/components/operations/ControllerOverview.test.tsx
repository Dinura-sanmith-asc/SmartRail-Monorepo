import { act, cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { server } from '../../../src/test/msw'
import { ControllerOverview } from '../../../src/features/operations/ControllerOverview'
import { colomboToday, filterSortOverviewJourneys } from '../../../src/features/operations/controllerOverviewModel'
import type { OperationsOverview, OperationsOverviewJourney } from '../../../src/features/operations/operationsApi'

const baseUrl = 'https://api.test'
const options = { apiBaseUrl: baseUrl, accessToken: 'controller-token' }

function journey(overrides: Partial<OperationsOverviewJourney> = {}): OperationsOverviewJourney {
  return {
    journeyId: 'journey-1', serviceId: 'internal-service-id', serviceCode: 'SERVICE-CODE-1',
    serviceDisplayName: 'Coastal Express', publicServiceNumber: 'EXP-42', routeCode: 'R-1', routeName: 'Coastal Route',
    operationDate: '2026-11-02', origin: 'Origin', destination: 'Destination',
    scheduledDeparture: '2026-11-02T08:00:00+05:30', scheduledArrival: '2026-11-02T12:00:00+05:30',
    status: 'Running', recordedDelayMinutes: 12, lastReportedStation: 'Harbor Station',
    lastUpdateAt: '2026-11-02T03:00:00Z', assignedDriver: null,
    ...overrides,
  }
}

function overview(date: string): OperationsOverview {
  return {
    operationalDate: date,
    metrics: { totalScheduled: 2, running: 1, delayed: 1, openIncidents: 3, cancelled: 1, completed: 1 },
    journeys: [journey({ operationDate: date }), journey({
      journeyId: 'journey-2', serviceId: 'internal-service-id-2', serviceCode: 'SERVICE-CODE-2',
      serviceDisplayName: null, publicServiceNumber: null, routeCode: 'R-2', routeName: 'Inland Route',
      operationDate: date, status: 'Scheduled', recordedDelayMinutes: null, lastReportedStation: null,
      scheduledDeparture: null, scheduledArrival: null,
    })],
  }
}

function renderOverview(props: Partial<React.ComponentProps<typeof ControllerOverview>> = {}) {
  const onOpenJourney = vi.fn()
  const onOpenIncidents = vi.fn()
  const onUnauthorized = vi.fn()
  const result = render(<ControllerOverview options={options} onOpenJourney={onOpenJourney} onOpenIncidents={onOpenIncidents} onUnauthorized={onUnauthorized} {...props} />)
  return { ...result, onOpenJourney, onOpenIncidents, onUnauthorized }
}

afterEach(() => vi.useRealTimers())

describe('Controller Railway Control Centre', () => {
  it('uses Colombo today, loads six server metrics globally, and distinguishes train identity fields', async () => {
    const requested: URL[] = []
    server.use(http.get(`${baseUrl}/api/operations/overview`, ({ request }) => {
      const url = new URL(request.url)
      requested.push(url)
      return HttpResponse.json(overview(url.searchParams.get('date')!))
    }))
    const fixtureToday = colomboToday(new Date('2026-11-01T20:00:00Z'))
    expect(fixtureToday).toBe('2026-11-02')

    renderOverview()

    expect(await screen.findByRole('heading', { name: 'Live Journey Board' })).toBeTruthy()
    expect(requested[0]?.searchParams.get('date')).toBe(colomboToday())
    expect(requested[0]?.searchParams.has('stationId')).toBe(false)
    expect(screen.getByRole('button', { name: /Scheduled Journeys Today: 2/ })).toBeTruthy()
    expect(screen.getByRole('button', { name: /Currently Running: 1/ })).toBeTruthy()
    expect(screen.getByRole('button', { name: /Delayed Journeys: 1/ })).toBeTruthy()
    expect(screen.getByRole('button', { name: /Open Incidents — Current/ })).toBeTruthy()
    expect(screen.getByText('Reported and In Progress incidents, independent of the selected journey date.')).toBeTruthy()
    expect(screen.getByText('Coastal Express')).toBeTruthy()
    expect(screen.getByText('Train No: EXP-42')).toBeTruthy()
    expect(screen.getAllByText(/Service ID:/)).toHaveLength(2)
    expect(screen.getByText('internal-service-id')).toBeTruthy()
    expect(screen.getByText('Service name unavailable')).toBeTruthy()
    expect(screen.getByText('Train No: Train number unavailable')).toBeTruthy()
    expect(screen.getByText('Service code: SERVICE-CODE-2')).toBeTruthy()
    expect(screen.getByText('12 min')).toBeTruthy()
    expect(screen.getByText('Delay unknown')).toBeTruthy()
    expect(screen.getByText('Harbor Station')).toBeTruthy()
    expect(screen.getByText('Not reported')).toBeTruthy()
  })

  it('requests the selected operating date and never adds the station-board filter', async () => {
    const dates: string[] = []
    server.use(http.get(`${baseUrl}/api/operations/overview`, ({ request }) => {
      const url = new URL(request.url)
      dates.push(`${url.searchParams.get('date')}|${url.searchParams.get('stationId') ?? ''}`)
      return HttpResponse.json(overview(url.searchParams.get('date')!))
    }))
    renderOverview()
    await screen.findByRole('heading', { name: 'Live Journey Board' })
    fireEvent.change(screen.getByLabelText('Operating date (Asia/Colombo)'), { target: { value: '2026-11-02' } })
    await waitFor(() => expect(dates).toContain('2026-11-02|'))
  })

  it('renders the backend empty-date result without inventing journeys', async () => {
    server.use(http.get(`${baseUrl}/api/operations/overview`, ({ request }) => HttpResponse.json({
      operationalDate: new URL(request.url).searchParams.get('date'),
      metrics: { totalScheduled: 0, running: 0, delayed: 0, openIncidents: 0, cancelled: 0, completed: 0 },
      journeys: [],
    })))
    renderOverview()
    expect(await screen.findByText('No journeys for this operating date')).toBeTruthy()
    expect(screen.getByRole('button', { name: /Scheduled Journeys Today: 0/ })).toBeTruthy()
  })

  it('searches and filters independently from lifecycle presentation and opens journey details', async () => {
    const user = userEvent.setup()
    const opened = vi.fn()
    server.use(http.get(`${baseUrl}/api/operations/overview`, ({ request }) => HttpResponse.json(overview(new URL(request.url).searchParams.get('date')!))))
    renderOverview({ onOpenJourney: opened })
    await screen.findByText('Coastal Express')

    await user.selectOptions(screen.getByLabelText('Journey status'), 'delayed')
    expect(screen.getByText('Coastal Express')).toBeTruthy()
    expect(screen.queryByText('Service name unavailable')).toBeNull()
    await user.clear(screen.getByLabelText('Search journeys'))
    await user.type(screen.getByLabelText('Search journeys'), 'Inland Route')
    await user.selectOptions(screen.getByLabelText('Journey status'), 'all')
    expect(screen.getByText('Service name unavailable')).toBeTruthy()
    expect(screen.queryByText('Coastal Express')).toBeNull()
    await user.clear(screen.getByLabelText('Search journeys'))
    await user.selectOptions(screen.getByLabelText('Journey status'), 'Scheduled')
    await user.click(screen.getByRole('button', { name: /View details/ }))
    expect(opened).toHaveBeenCalledWith('journey-2')
  })

  it('sorts journeys by the selected table column and routes the current-open-incidents card', async () => {
    const user = userEvent.setup()
    server.use(http.get(`${baseUrl}/api/operations/overview`, ({ request }) => HttpResponse.json(overview(new URL(request.url).searchParams.get('date')!))))
    const { onOpenIncidents } = renderOverview()
    await screen.findByText('Coastal Express')
    const board = screen.getByRole('table', { name: /Live journey board/ })
    const rows = () => within(board).getAllByRole('row').slice(1).map((row) => row.textContent ?? '')
    expect(rows()[0]).toContain('Coastal Express')
    await user.click(screen.getByRole('button', { name: /Train name/ }))
    await user.click(screen.getByRole('button', { name: /Train name/ }))
    expect(rows()[0]).toContain('Service name unavailable')
    await user.click(screen.getByRole('button', { name: /Open Incidents — Current/ }))
    expect(onOpenIncidents).toHaveBeenCalledOnce()
  })

  it('preserves same-date results as stale and allows manual retry after an unavailable response', async () => {
    let reads = 0
    server.use(http.get(`${baseUrl}/api/operations/overview`, ({ request }) => {
      reads += 1
      return reads === 1 ? HttpResponse.json(overview(new URL(request.url).searchParams.get('date')!)) : HttpResponse.json({}, { status: 503 })
    }))
    renderOverview()
    await screen.findByText('Coastal Express')
    fireEvent.click(screen.getByRole('button', { name: /^Refresh$/ }))
    expect(await screen.findByText('Showing stale overview data')).toBeTruthy()
    expect(screen.getByText('Coastal Express')).toBeTruthy()
    expect(screen.getAllByText(/Last successful update:/).length).toBeGreaterThan(0)
  })

  it('invalidates the local session after 401 and shows a safe denial for 403', async () => {
    const unauthorized = vi.fn()
    server.use(http.get(`${baseUrl}/api/operations/overview`, () => HttpResponse.json({}, { status: 401 })))
    renderOverview({ onUnauthorized: unauthorized })
    expect(await screen.findByText('Overview unavailable')).toBeTruthy()
    await waitFor(() => expect(unauthorized).toHaveBeenCalledOnce())
    cleanup()

    server.use(http.get(`${baseUrl}/api/operations/overview`, () => HttpResponse.json({}, { status: 403 })))
    renderOverview()
    expect(await screen.findByText('The server denied this operation for your role or resource scope.')).toBeTruthy()
  })

  it('shows a safe connectivity failure without presenting a live state', async () => {
    server.use(http.get(`${baseUrl}/api/operations/overview`, () => HttpResponse.error()))
    renderOverview()
    expect(await screen.findByText('Overview unavailable')).toBeTruthy()
    expect(screen.getByText('A network connection is unavailable. Check your connection and try again.')).toBeTruthy()
    expect(screen.queryByText(/LIVE · REFRESHES EVERY 30 SECONDS/)).toBeNull()
  })

  it('refreshes automatically every thirty seconds', async () => {
    vi.useFakeTimers({ toFake: ['setInterval', 'clearInterval'] })
    let reads = 0
    server.use(http.get(`${baseUrl}/api/operations/overview`, ({ request }) => {
      reads += 1
      return HttpResponse.json(overview(new URL(request.url).searchParams.get('date')!))
    }))
    renderOverview()
    await screen.findByText('Coastal Express')
    expect(reads).toBe(1)
    await act(async () => { vi.advanceTimersByTime(30_000) })
    await waitFor(() => expect(reads).toBe(2))
  })
})

describe('overview journey filtering', () => {
  it('keeps Running journeys visible in the delayed filter when they carry authoritative delay', () => {
    const runningDelayed = journey({ status: 'Running', recordedDelayMinutes: 12 })
    const scheduled = journey({ journeyId: 'scheduled', status: 'Scheduled', recordedDelayMinutes: null })
    expect(filterSortOverviewJourneys([runningDelayed, scheduled], '', 'delayed', 'departure', 'ascending')).toEqual([runningDelayed])
  })
})
