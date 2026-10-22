const OAUTH_RESPONSE_PARAMETERS = ['code', 'state', 'error', 'error_description', 'error_uri', 'session_state']

export function clearOAuthResponseFromUrl(): void {
  const current = new URL(window.location.href)
  for (const parameter of OAUTH_RESPONSE_PARAMETERS) current.searchParams.delete(parameter)
  const safePath = `${current.pathname}${current.search}${current.hash}`
  window.history.replaceState(window.history.state, document.title, safePath)
}
