import { createBrowserRouter, Navigate, RouterProvider } from 'react-router'
import { CallbackPage } from '../features/staff-access/auth/CallbackPage'
import { SignInPage } from '../features/staff-access/auth/SignInPage'
import { ProtectedRoute } from '../features/staff-access/routes/ProtectedRoute'
import { ControllerWorkspace } from '../features/staff-access/workspaces/ControllerWorkspace'

const router = createBrowserRouter([
  { path: '/', element: <SignInPage /> },
  { path: '/auth/callback', element: <CallbackPage /> },
  {
    path: '/controller/*',
    element: (
      <ProtectedRoute requiredRole="Controller">
        <ControllerWorkspace />
      </ProtectedRoute>
    ),
  },
  { path: '*', element: <Navigate to="/" replace /> },
])

export function AppRouter() {
  return <RouterProvider router={router} />
}
