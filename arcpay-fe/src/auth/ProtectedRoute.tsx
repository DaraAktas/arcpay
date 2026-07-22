import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from './auth-context'
import { LoadingScreen } from '../components/LoadingScreen'

export function ProtectedRoute() {
  const { status } = useAuth()
  const location = useLocation()

  if (status === 'checking') return <LoadingScreen />
  if (status === 'anonymous') {
    return <Navigate to="/giris" replace state={{ from: location.pathname }} />
  }

  return <Outlet />
}
