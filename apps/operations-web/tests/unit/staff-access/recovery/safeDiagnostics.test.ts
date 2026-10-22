import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiRequestError, fetchJson } from '../../../../src/shared/api/fetchJson'
import { toSafeUserMessage } from '../../../../src/shared/api/safeError'

const sensitiveFailureDetails = {
  access_token: 'access-secret-value',
  id_token: 'id-secret-value',
  refresh_token: 'refresh-secret-value',
  claims: { sub: 'private-subject', email: 'private@example.test', groups: ['internal-admin'] },
  authorization: 'Bearer authorization-secret',
  policy: 'ControllerOnlyPolicy internal authorization details',
  problem: {
    type: 'https://internal.example.test/problems/authorization',
    title: 'Internal access policy failure',
    status: 403,
    detail: 'private station scope denied for station STN-SECRET',
    instance: '/internal/trace/trace-secret',
    traceId: 'trace-secret',
  },
}

const sensitiveValues = [
  'access-secret-value',
  'id-secret-value',
  'refresh-secret-value',
  'private-subject',
  'private@example.test',
  'internal-admin',
  'authorization-secret',
  'ControllerOnlyPolicy',
  'internal authorization details',
  'Internal access policy failure',
  'private station scope denied',
  'STN-SECRET',
  'trace-secret',
]

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('safe failure output', () => {
  it('keeps user-facing messages generic when failures contain sensitive details', () => {
    const serializedDetails = JSON.stringify(sensitiveFailureDetails)
    const messages = [
      toSafeUserMessage('unauthenticated', sensitiveFailureDetails),
      toSafeUserMessage('forbidden', serializedDetails),
      toSafeUserMessage('authority-unavailable', sensitiveFailureDetails.problem),
      toSafeUserMessage('service-error', serializedDetails),
      toSafeUserMessage('malformed-response', sensitiveFailureDetails),
    ]

    for (const output of messages) {
      for (const value of sensitiveValues) expect(output).not.toContain(value)
    }
  })

  it('keeps safe diagnostics limited to classified failure data when a future operation error carries raw Problem Details', async () => {
    const rawProblem = JSON.stringify(sensitiveFailureDetails.problem)
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(rawProblem, {
      status: 403,
      headers: { 'Content-Type': 'application/problem+json' },
    })))

    let captured: unknown
    try {
      await fetchJson('')
    } catch (error) {
      captured = error
    }

    expect(captured).toBeInstanceOf(ApiRequestError)
    const failure = captured as ApiRequestError
    const safeDiagnostic = JSON.stringify({ name: failure.name, failure: failure.failure, status: failure.status })
    const renderedMessage = toSafeUserMessage(failure.failure, failure)

    expect(safeDiagnostic).toContain('forbidden')
    expect(safeDiagnostic).toContain('403')
    for (const value of sensitiveValues) {
      expect(safeDiagnostic).not.toContain(value)
      expect(renderedMessage).not.toContain(value)
    }
    expect(failure.message).toBe('forbidden')
  })
})
