import { createContext, useContext } from 'react'

export type LocalSessionContextValue = {
  available: boolean
  isTerminated: boolean
  invalidateLocalSession: () => void
  remoteLogoutUrl: string | null
  removeUser: () => Promise<unknown>
}

export const LocalSessionContext = createContext<LocalSessionContextValue>({
  available: false,
  isTerminated: false,
  invalidateLocalSession: () => undefined,
  remoteLogoutUrl: null,
  removeUser: async () => undefined,
})

export function useLocalSession() {
  return useContext(LocalSessionContext)
}
