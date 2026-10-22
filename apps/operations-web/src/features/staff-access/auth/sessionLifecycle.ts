export type ExpirySubscription = (onExpired: () => void) => () => void

/** Bind Cognito's access-token expiry event to clearing the local OIDC session. */
export function registerAccessTokenExpiry(subscribe: ExpirySubscription, clearLocalSession: () => void) {
  return subscribe(clearLocalSession)
}
