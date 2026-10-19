import { useEffect, useReducer, type ReactNode } from 'react'
import { ApiRequestError } from '../../../shared/api/fetchJson'
import { getCurrentIdentity } from './currentIdentityClient'
import { IdentityContext } from './identityContext'
import { initialStoredIdentityState, reduceIdentityState, stateForCurrentSession } from './identityState'

export function IdentityProvider({
  apiBaseUrl,
  accessToken,
  children,
}: {
  apiBaseUrl: string
  accessToken: string | null
  children: ReactNode
}) {
  const [stored, dispatch] = useReducer(reduceIdentityState, initialStoredIdentityState)
  useEffect(() => {
    if (!accessToken) {
      dispatch({ type: 'set', accessToken: null, state: { status: 'unauthenticated' } })
      return
    }

    const controller = new AbortController()
    let active = true
    dispatch({ type: 'set', accessToken, state: { status: 'loading' } })

    void getCurrentIdentity({ apiBaseUrl, accessToken, signal: controller.signal }).then(
      (identity) => {
        if (!active || controller.signal.aborted) return
        dispatch({ type: 'set', accessToken, state: { status: 'authenticated', identity } })
      },
      (error: unknown) => {
        if (!active || controller.signal.aborted) return
        if (error instanceof ApiRequestError && error.failure === 'unauthenticated') {
          dispatch({ type: 'set', accessToken, state: { status: 'reauthentication-required' } })
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
  }, [accessToken, apiBaseUrl])

  const state = stateForCurrentSession(stored, accessToken)

  return <IdentityContext.Provider value={{ state }}>{children}</IdentityContext.Provider>
}
