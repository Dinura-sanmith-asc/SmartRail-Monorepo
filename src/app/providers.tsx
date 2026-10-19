import type { ReactNode } from 'react'
import { useMemo } from 'react'
import { parseRuntimeConfig, RuntimeConfigError } from '../config/runtimeConfig'

export function RuntimeConfigProvider({ children }: { children: ReactNode }) {
  const result = useMemo(() => {
    try {
      return { config: parseRuntimeConfig(import.meta.env as Record<string, unknown>), error: null }
    } catch (error) {
      return {
        config: null,
        error: error instanceof RuntimeConfigError ? error.message : 'Application configuration is unavailable.',
      }
    }
  }, [])

  if (result.error) {
    return (
      <main className="startup-error" role="alert">
        <h1>SmartRail Operations Web is unavailable</h1>
        <p>{result.error}</p>
        <p>Contact your system administrator for assistance.</p>
      </main>
    )
  }

  return <>{children}</>
}
