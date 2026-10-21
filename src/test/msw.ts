import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'

/** Default contract fixture for the approved authenticated GET /api/me operation. */
export const server = setupServer(
  http.get('*/api/me', () =>
    HttpResponse.json({
      subject: 'test-cognito-subject',
      roles: [],
    }),
  ),
  http.get('https://api.test/api/stations', () => HttpResponse.json({ items: [] })),
  http.get('https://api.test/api/stations/:id/departures', () => HttpResponse.json([])),
  http.get('https://api.test/api/incidents', () => HttpResponse.json({ items: [] })),
  http.get('https://api.test/api/operations/overview', ({ request }) => HttpResponse.json({
    operationalDate: new URL(request.url).searchParams.get('date'),
    metrics: { totalScheduled: 0, running: 0, delayed: 0, openIncidents: 0, cancelled: 0, completed: 0 },
    journeys: [],
  })),
)
