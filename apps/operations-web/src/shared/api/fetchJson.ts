import { classifyApiFailure, type ApiFailure, type SafeFailureKind } from './safeError'

export class ApiRequestError extends Error {
  readonly failure: SafeFailureKind
  readonly status?: number
  readonly backendMessage?: string

  constructor(failure: SafeFailureKind, status?: number, backendMessage?: string) {
    super(failure)
    this.name = 'ApiRequestError'
    this.failure = failure
    this.status = status
    this.backendMessage = backendMessage
  }
}

export async function fetchJson<T>(url: string, init: RequestInit = {}, timeoutMs = 15_000): Promise<T> {
  const controller = new AbortController()
  const externalSignal = init.signal
  const abortFromExternal = () => controller.abort(externalSignal?.reason)
  if (externalSignal?.aborted) abortFromExternal()
  else externalSignal?.addEventListener('abort', abortFromExternal, { once: true })
  const timeout = setTimeout(() => controller.abort('timeout'), timeoutMs)

  try {
    const response = await fetch(url, { ...init, signal: controller.signal })
    if (!response.ok) {
      const failure: ApiFailure = { kind: 'http', status: response.status }
      let backendMessage: string | undefined
      try {
        const body: unknown = await response.json()
        if (typeof body === 'object' && body !== null && 'title' in body && typeof body.title === 'string') {
          backendMessage = body.title
          if ('errors' in body && typeof body.errors === 'object' && body.errors !== null) {
            const details = Object.values(body.errors).flatMap((messages) => Array.isArray(messages) ? messages.filter((message): message is string => typeof message === 'string') : [])
            if (details.length) backendMessage = `${backendMessage} ${details.join(' ')}`
          }
        }
      } catch { /* An empty or non-JSON problem body still uses the safe generic message. */ }
      throw new ApiRequestError(classifyApiFailure(failure), response.status, backendMessage)
    }
    if (response.status === 204) return undefined as T
    try {
      return (await response.json()) as T
    } catch {
      throw new ApiRequestError(classifyApiFailure({ kind: 'malformed' }))
    }
  } catch (error) {
    if (error instanceof ApiRequestError) throw error
    if (controller.signal.aborted) {
      if (externalSignal?.aborted) throw error
      throw new ApiRequestError(classifyApiFailure({ kind: 'timeout' }))
    }
    throw new ApiRequestError(classifyApiFailure({ kind: 'network' }))
  } finally {
    clearTimeout(timeout)
    externalSignal?.removeEventListener('abort', abortFromExternal)
  }
}
