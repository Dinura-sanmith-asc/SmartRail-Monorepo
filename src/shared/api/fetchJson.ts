import { classifyApiFailure, type ApiFailure, type SafeFailureKind } from './safeError'

export class ApiRequestError extends Error {
  readonly failure: SafeFailureKind
  readonly status?: number

  constructor(failure: SafeFailureKind, status?: number) {
    super(failure)
    this.name = 'ApiRequestError'
    this.failure = failure
    this.status = status
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
      throw new ApiRequestError(classifyApiFailure(failure), response.status)
    }
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
