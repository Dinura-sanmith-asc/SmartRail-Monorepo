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
  http.get('https://api.test/api/incidents', () => HttpResponse.json({ items: [] })),
)
