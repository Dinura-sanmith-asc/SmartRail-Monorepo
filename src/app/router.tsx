import { createBrowserRouter, RouterProvider } from 'react-router'

function FoundationPage() {
  return (
    <main className="app-shell">
      <header><p className="app-shell__eyebrow">SmartRail</p><h1>Railway Operations</h1></header>
      <section aria-labelledby="welcome-heading">
        <h2 id="welcome-heading">Operations Web foundation</h2>
        <p>Staff access is not configured yet. Sign-in and operational workspaces will be available in a later implementation milestone.</p>
      </section>
    </main>
  )
}

const router = createBrowserRouter([{ path: '*', element: <FoundationPage /> }])

export function AppRouter() {
  return <RouterProvider router={router} />
}
