import type { IdentityState } from './identityContext'

export type StoredIdentityState = {
  accessToken: string | null
  state: IdentityState
}

export type IdentityStateAction = {
  type: 'set'
  accessToken: string | null
  state: IdentityState
}

export const initialStoredIdentityState: StoredIdentityState = {
  accessToken: null,
  state: { status: 'unauthenticated' },
}

export function reduceIdentityState(_current: StoredIdentityState, action: IdentityStateAction): StoredIdentityState {
  return { accessToken: action.accessToken, state: action.state }
}

/** Hide any previously resolved identity immediately when the session token changes. */
export function stateForCurrentSession(stored: StoredIdentityState, accessToken: string | null): IdentityState {
  if (stored.accessToken === accessToken) return stored.state
  return accessToken ? { status: 'loading' } : { status: 'unauthenticated' }
}
