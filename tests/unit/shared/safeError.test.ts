import { describe, expect, it } from 'vitest'
import { classifyApiFailure, toSafeUserMessage } from '../../../src/shared/api/safeError'

describe('classifyApiFailure', () => {
  it.each([
    [401, 'unauthenticated'],
    [403, 'forbidden'],
    [503, 'authority-unavailable'],
    [500, 'service-error'],
  ] as const)('maps HTTP %i to %s', (status, expected) => {
    expect(classifyApiFailure({ kind: 'http', status })).toBe(expected)
  })

  it('distinguishes a request timeout from offline/network failure', () => {
    expect(classifyApiFailure({ kind: 'timeout' })).toBe('timeout')
    expect(classifyApiFailure({ kind: 'network' })).toBe('offline')
  })

  it('classifies malformed successful responses without trusting their body', () => {
    expect(classifyApiFailure({ kind: 'malformed' })).toBe('malformed-response')
  })
})

describe('toSafeUserMessage', () => {
  it('returns a generic message and never exposes token, claim, or backend details', () => {
    const secrets = ['Bearer secret-token', 'private-sub', 'postgres password failed']
    const message = toSafeUserMessage('service-error', secrets.join(' '))
    expect(message).not.toContain('secret-token')
    expect(message).not.toContain('private-sub')
    expect(message).not.toContain('postgres')
  })
})
