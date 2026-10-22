import { createContext, useContext } from 'react'
import type { SafeFailureKind } from '../../../shared/api/safeError'
import type { CurrentIdentity } from './currentIdentity'

export type IdentityState =
  | { status: 'loading' }
  | { status: 'authenticated'; identity: CurrentIdentity }
  | { status: 'unauthenticated' }
  | { status: 'no-operational-access' }
  | { status: 'reauthentication-required' }
  | { status: 'authority-unavailable' }
  | { status: 'error'; failure: SafeFailureKind }

export type IdentityContextValue = { state: IdentityState; retryIdentity?: () => void }

export const IdentityContext = createContext<IdentityContextValue>({ state: { status: 'loading' }, retryIdentity: () => undefined })

export function useIdentity() {
  const context = useContext(IdentityContext)
  return {
    ...context,
    retryIdentity: context.retryIdentity ?? (() => undefined),
    identity: context.state.status === 'authenticated' ? context.state.identity : null,
  }
}
