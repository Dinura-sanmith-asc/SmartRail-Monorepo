import { ApiRequestError, fetchJson } from '../../../shared/api/fetchJson'
import { parseCurrentIdentity, type CurrentIdentity } from './currentIdentity'

export type GetCurrentIdentityOptions = {
  apiBaseUrl: string
  accessToken: string
  timeoutMs?: number
  signal?: AbortSignal
}

export async function getCurrentIdentity({ apiBaseUrl, accessToken, timeoutMs = 15_000, signal }: GetCurrentIdentityOptions): Promise<CurrentIdentity> {
  let body: unknown
  try {
    body = await fetchJson<unknown>(
      `${apiBaseUrl.replace(/\/$/, '')}/api/me`,
      {
        method: 'GET',
        headers: {
          Accept: 'application/json',
          Authorization: `Bearer ${accessToken}`,
        },
        signal,
      },
      timeoutMs,
    )
  } catch (error) {
    if (error instanceof ApiRequestError) {
      // The approved identity operation defines 401 and 503, not 403.
      if (error.status === 403) throw new ApiRequestError('service-error', 403)
      throw error
    }
    throw new ApiRequestError('malformed-response')
  }

  try {
    return parseCurrentIdentity(body)
  } catch {
    throw new ApiRequestError('malformed-response')
  }
}
