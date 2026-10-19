import './AccessState.css'

export type AccessStateKind = 'loading' | 'no-access' | 'forbidden' | 'unavailable' | 'offline'

const copy: Record<AccessStateKind, { text: string; role: 'status' | 'alert' }> = {
  loading: { text: 'Loading your access information…', role: 'status' },
  'no-access': { text: 'You have no access to this workspace.', role: 'alert' },
  forbidden: { text: 'Access denied. You do not have permission to perform this action.', role: 'alert' },
  unavailable: { text: 'Access information is temporarily unavailable.', role: 'status' },
  offline: { text: 'A network connection is unavailable.', role: 'alert' },
}

export function AccessState({ state, onRetry }: { state: AccessStateKind; onRetry?: () => void }) {
  const message = copy[state]
  return (
    <section className="access-state" aria-live={message.role === 'alert' ? 'assertive' : 'polite'}>
      <p role={message.role}>{message.text}</p>
      {onRetry && state !== 'loading' && (
        <button type="button" className="access-state__retry" onClick={onRetry}>
          Try again
        </button>
      )}
    </section>
  )
}
