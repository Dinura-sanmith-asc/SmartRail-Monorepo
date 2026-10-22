export type ApiFailure =
  | { kind: 'http'; status: number }
  | { kind: 'timeout' }
  | { kind: 'network' }
  | { kind: 'malformed' }

export type SafeFailureKind =
  | 'unauthenticated'
  | 'forbidden'
  | 'authority-unavailable'
  | 'service-error'
  | 'timeout'
  | 'offline'
  | 'malformed-response'

export function classifyApiFailure(failure: ApiFailure): SafeFailureKind {
  if (failure.kind === 'timeout') return 'timeout'
  if (failure.kind === 'network') return 'offline'
  if (failure.kind === 'malformed') return 'malformed-response'
  if (failure.status === 401) return 'unauthenticated'
  if (failure.status === 403) return 'forbidden'
  if (failure.status === 503) return 'authority-unavailable'
  return 'service-error'
}

const messages: Record<SafeFailureKind, string> = {
  unauthenticated: 'Your session has expired. Sign in again to continue.',
  forbidden: 'Access denied. You do not have permission to perform this action.',
  'authority-unavailable': 'Access information is temporarily unavailable. Try again shortly.',
  'service-error': 'The request could not be completed. Try again later.',
  timeout: 'The request took too long. Try again.',
  offline: 'A network connection is unavailable. Check your connection and try again.',
  'malformed-response': 'The service returned an unexpected response. Try again later.',
}

export function toSafeUserMessage(kind: SafeFailureKind, details?: unknown): string {
  void details
  return messages[kind]
}
