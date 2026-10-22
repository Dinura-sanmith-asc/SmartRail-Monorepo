import { describe, expect, it, vi } from 'vitest'
import { registerAccessTokenExpiry } from '../../../../src/features/staff-access/auth/sessionLifecycle'

describe('access-token expiry lifecycle', () => {
  it('clears the local OIDC session when the access-token-expired event fires', () => {
    let onExpired: (() => void) | undefined
    const unsubscribe = vi.fn()
    const subscribe = vi.fn((handler: () => void) => {
      onExpired = handler
      return unsubscribe
    })
    const clearLocalSession = vi.fn()

    const removeListener = registerAccessTokenExpiry(subscribe, clearLocalSession)
    expect(subscribe).toHaveBeenCalledOnce()
    expect(clearLocalSession).not.toHaveBeenCalled()

    onExpired?.()

    expect(clearLocalSession).toHaveBeenCalledOnce()
    removeListener()
    expect(unsubscribe).toHaveBeenCalledOnce()
  })
})
