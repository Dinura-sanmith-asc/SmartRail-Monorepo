import { AccessState, type AccessStateKind } from '../../../shared/ui/AccessState'
import type { IdentityState } from './identityContext'
import { useIdentity } from './identityContext'

type RecoverableIdentityState = Extract<IdentityState, { status: 'authority-unavailable' | 'error' | 'no-operational-access' }>

function stateKind(state: RecoverableIdentityState): AccessStateKind {
  if (state.status === 'authority-unavailable') return 'unavailable'
  if (state.status === 'no-operational-access') return 'no-access'
  switch (state.failure) {
    case 'forbidden': return 'forbidden'
    case 'offline': return 'offline'
    case 'timeout': return 'timeout'
    case 'malformed-response': return 'malformed'
    case 'unauthenticated': return 'service-error'
    case 'authority-unavailable': return 'unavailable'
    case 'service-error': return 'service-error'
  }
}

export function IdentityRecovery({ state }: { state: RecoverableIdentityState }) {
  const { retryIdentity } = useIdentity()
  const retryable = state.status === 'authority-unavailable' ||
    (state.status === 'error' && state.failure !== 'forbidden' && state.failure !== 'unauthenticated')

  return <AccessState state={stateKind(state)} onRetry={retryable ? retryIdentity : undefined} />
}
