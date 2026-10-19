export type SignOutDependencies = {
  remoteLogoutUrl: string | null
  invalidateLocalSession: () => void
  clearTransactionState: () => void
  removeUser: () => Promise<unknown>
  navigateToSignIn: () => void
  navigateToRemoteLogout: (url: string) => void | Promise<void>
  onRemoteLogoutUnavailable: () => void
}

/**
 * Creates a single-flight sign-out operation. Local access is invalidated before
 * OIDC cleanup or any browser navigation; failures never restore the session.
 */
export function createSignOutHandler(dependencies: SignOutDependencies): () => Promise<void> {
  let inFlight: Promise<void> | null = null
  let completed = false

  return () => {
    if (completed) return Promise.resolve()
    if (inFlight) return inFlight

    inFlight = (async () => {
      dependencies.invalidateLocalSession()
      try {
        dependencies.clearTransactionState()
      } catch {
        // A failed temporary-state cleanup must not preserve application access.
      }

      try {
        await dependencies.removeUser()
      } catch {
        // LocalSessionContext already closed protected routes synchronously.
      }

      dependencies.navigateToSignIn()
      if (!dependencies.remoteLogoutUrl) {
        dependencies.onRemoteLogoutUnavailable()
        completed = true
        return
      }

      try {
        await dependencies.navigateToRemoteLogout(dependencies.remoteLogoutUrl)
      } catch {
        dependencies.onRemoteLogoutUnavailable()
      }
      completed = true
    })().finally(() => {
      inFlight = null
    })

    return inFlight
  }
}
