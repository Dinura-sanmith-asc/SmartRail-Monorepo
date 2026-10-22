import { useCallback, useEffect, useReducer, useState, type ReactNode } from 'react'
import { ApiRequestError } from '../../../shared/api/fetchJson'
import { getCurrentIdentity } from './currentIdentityClient'
import { IdentityContext } from './identityContext'
import { initialStoredIdentityState, reduceIdentityState, stateForCurrentSession } from './identityState'

export function IdentityProvider({
  apiBaseUrl,
  accessToken,
  timeoutMs = 15_000,
  onAuthenticationRequired,
  children,
}: {
  apiBaseUrl: string
  accessToken: string | null
  timeoutMs?: number
  onAuthenticationRequired?: () => void
  children: ReactNode
}) {
  const [stored, dispatch] = useReducer(reduceIdentityState, initialStoredIdentityState)
  const [retryVersion, setRetryVersion] = useState(0)
  const retryIdentity = useCallback(() => setRetryVersion((version) => version + 1), [])
  useEffect(() => {
    if (!accessToken) {
      dispatch({ type: 'set', accessToken: null, state: { status: 'unauthenticated' } })
      return
    }

    const controller = new AbortController()
    let active = true
    dispatch({ type: 'set', accessToken, state: { status: 'loading' } })

    void getCurrentIdentity({ apiBaseUrl, accessToken, signal: controller.signal, timeoutMs }).then(
      (identity) => {
        if (!active || controller.signal.aborted) return
        dispatch({ type: 'set', accessToken, state: { status: 'authenticated', identity } })
      },
      (error: unknown) => {
        if (!active || controller.signal.aborted) return
        if (error instanceof ApiRequestError && error.failure === 'unauthenticated') {
          dispatch({ type: 'set', accessToken, state: { status: 'reauthentication-required' } })
          onAuthenticationRequired?.()
          return
        }
        if (error instanceof ApiRequestError && error.failure === 'authority-unavailable') {
          dispatch({ type: 'set', accessToken, state: { status: 'authority-unavailable' } })
          return
        }
        dispatch({
          type: 'set',
          accessToken,
          state: {
            status: 'error',
            failure: error instanceof ApiRequestError ? error.failure : 'service-error',
          },
        })
      },
    )

    return () => {
      active = false
      controller.abort()
    }
  }, [accessToken, apiBaseUrl, onAuthenticationRequired, retryVersion, timeoutMs])

  const state = stateForCurrentSession(stored, accessToken)

  return <IdentityContext.Provider value={{ state, retryIdentity }}>{children}</IdentityContext.Provider>
}
