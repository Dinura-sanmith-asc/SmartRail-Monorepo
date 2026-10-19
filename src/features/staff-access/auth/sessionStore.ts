import type { StateStore } from 'oidc-client-ts'
import { WebStorageStateStore } from 'oidc-client-ts'

/** Holds the OIDC User object, including bearer tokens, for this page lifetime only. */
export class InMemoryStateStore implements StateStore {
  private readonly values = new Map<string, string>()

  async set(key: string, value: string): Promise<void> {
    this.values.set(key, value)
  }

  async get(key: string): Promise<string | null> {
    return this.values.get(key) ?? null
  }

  async remove(key: string): Promise<string | null> {
    const value = this.values.get(key) ?? null
    this.values.delete(key)
    return value
  }

  async getAllKeys(): Promise<string[]> {
    return [...this.values.keys()]
  }
}

/**
 * OIDC's short-lived redirect transaction (state, nonce, and PKCE verifier) must
 * survive Cognito's full-page redirect. This store is separate from userStore.
 */
export function createTransactionStateStore(): StateStore {
  if (typeof window === 'undefined' || !window.sessionStorage) {
    throw new Error('Browser session storage is required to validate the sign-in redirect transaction.')
  }
  return new WebStorageStateStore({ store: window.sessionStorage, prefix: 'smartrail.oidc.transaction.' })
}

/** Remove only temporary OIDC redirect transactions, never token-bearing user data. */
export function clearTransactionState(): void {
  if (typeof window === 'undefined') return
  try {
    const storage = window.sessionStorage
    const prefix = 'smartrail.oidc.transaction.'
    for (let index = storage.length - 1; index >= 0; index -= 1) {
      const key = storage.key(index)
      if (key?.startsWith(prefix)) storage.removeItem(key)
    }
  } catch {
    // Storage may be unavailable; callers still invalidate local access.
  }
}
