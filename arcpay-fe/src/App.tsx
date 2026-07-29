import { Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './auth/AuthContext'
import { ProtectedRoute } from './auth/ProtectedRoute'
import { DashboardPage } from './pages/DashboardPage'
import { LoginPage } from './pages/LoginPage'
import { RegisterPage } from './pages/RegisterPage'
import { InvestmentPage } from './pages/InvestmentPage'

export default function App() {
  return (
    <AuthProvider>
      <Routes>
        <Route path="/" element={<Navigate to="/hesabim" replace />} />
        <Route path="/giris" element={<LoginPage />} />
        <Route path="/kayit" element={<RegisterPage />} />
        <Route element={<ProtectedRoute />}>
          <Route path="/hesabim" element={<DashboardPage />} />
          <Route path="/yatirimlar" element={<InvestmentPage />} />
        </Route>
        <Route path="*" element={<Navigate to="/hesabim" replace />} />
      </Routes>
    </AuthProvider>
  )
}
