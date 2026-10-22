import { createContext, useContext } from 'react'
import type { PublicRuntimeConfig } from './runtimeConfig'

export const RuntimeConfigContext = createContext<PublicRuntimeConfig | null>(null)

export function useRuntimeConfig(): PublicRuntimeConfig {
  const config = useContext(RuntimeConfigContext)
  if (!config) throw new Error('RuntimeConfigProvider is required')
  return config
}
