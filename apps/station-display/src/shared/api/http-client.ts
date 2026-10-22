export interface ProblemDetails {
  type: string
  title: string
  status: number
  detail?: string
  instance?: string
  code?: string
  errors?: Record<string, string[]>
  traceId?: string
}

export type HttpClientFailureKind = 'network' | 'http' | 'malformed-response'

export class HttpClientError extends Error {
  readonly kind: HttpClientFailureKind
  readonly status?: number
  readonly problem?: ProblemDetails

  constructor(
    kind: HttpClientFailureKind,
    message: string,
    options: { status?: number; problem?: ProblemDetails } = {},
  ) {
    super(message)
    this.name = 'HttpClientError'
    this.kind = kind
    this.status = options.status
    this.problem = options.problem
  }
}

export interface HttpClientOptions {
  baseUrl: string
  fetchImpl?: typeof fetch
}

export interface HttpClient {
  get<T>(path: string, signal?: AbortSignal): Promise<T>
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

function isProblemDetails(value: unknown): value is ProblemDetails {
  if (!isRecord(value)) return false

  return (
    typeof value.type === 'string' &&
    typeof value.title === 'string' &&
    typeof value.status === 'number' &&
    Number.isInteger(value.status) &&
    (value.detail === undefined || typeof value.detail === 'string') &&
    (value.instance === undefined || typeof value.instance === 'string') &&
    (value.code === undefined || typeof value.code === 'string') &&
    (value.traceId === undefined || typeof value.traceId === 'string') &&
    (value.errors === undefined ||
      (isRecord(value.errors) &&
        Object.values(value.errors).every(
          (messages) => Array.isArray(messages) && messages.every((message) => typeof message === 'string'),
        )))
  )
}

function normalizePath(baseUrl: string, path: string): string {
  const base = baseUrl.endsWith('/') ? baseUrl : `${baseUrl}/`
  return new URL(path.replace(/^\/+/, ''), base).toString()
}

export function createHttpClient({ baseUrl, fetchImpl = fetch }: HttpClientOptions): HttpClient {
  return {
    async get<T>(path: string, signal?: AbortSignal): Promise<T> {
      let response: Response

      try {
        response = await fetchImpl(normalizePath(baseUrl, path), {
          method: 'GET',
          headers: { Accept: 'application/json, application/problem+json' },
          ...(signal ? { signal } : {}),
        })
      } catch {
        throw new HttpClientError('network', 'The request could not reach the server.')
      }

      let body: unknown
      try {
        body = await response.json()
      } catch {
        throw new HttpClientError('malformed-response', 'The server returned invalid JSON.')
      }

      if (!response.ok) {
        if (!isProblemDetails(body)) {
          throw new HttpClientError('malformed-response', 'The server returned an invalid Problem response.')
        }
        throw new HttpClientError('http', body.title, { status: response.status, problem: body })
      }

      return body as T
    },
  }
}
