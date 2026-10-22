import './AccessState.css'

export type AccessStateKind = 'loading' | 'no-access' | 'forbidden' | 'unavailable' | 'offline' | 'service-error' | 'timeout' | 'malformed'

const copy: Record<AccessStateKind, { heading: string; text: string; role: 'status' | 'alert' }> = {
  loading: { heading: 'Checking access', text: 'Loading your access information…', role: 'status' },
  'no-access': { heading: 'No operational access', text: 'You have no access to this workspace.', role: 'alert' },
  forbidden: { heading: 'Access denied', text: 'Access denied. You do not have permission to perform this action.', role: 'alert' },
  unavailable: { heading: 'Access information unavailable', text: 'Access information is temporarily unavailable.', role: 'status' },
  offline: { heading: 'Network unavailable', text: 'A network connection is unavailable. Check your connection and try again.', role: 'alert' },
  'service-error': { heading: 'Service problem', text: 'The request could not be completed. Try again later.', role: 'alert' },
  timeout: { heading: 'Request timed out', text: 'The request took too long. Try again.', role: 'alert' },
  malformed: { heading: 'Unexpected service response', text: 'The service returned an unexpected response. Try again later.', role: 'alert' },
}

export function AccessState({ state, onRetry }: { state: AccessStateKind; onRetry?: () => void }) {
  const message = copy[state]
  return (
    <section className="access-state" aria-labelledby="access-state-heading" aria-live={message.role === 'alert' ? 'assertive' : 'polite'}>
      <h2 id="access-state-heading">{message.heading}</h2>
      <p role={message.role}>{message.text}</p>
      {onRetry && state !== 'loading' && (
        <button type="button" className="access-state__retry" onClick={onRetry}>
          Try again
        </button>
      )}
    </section>
  )
}
