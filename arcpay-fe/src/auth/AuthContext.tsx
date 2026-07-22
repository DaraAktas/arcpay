import { useCallback, useEffect, useMemo, useState, type PropsWithChildren } from 'react'
import { customerApi } from '../lib/api'
import {
  clearSession,
  readSession,
  saveSession,
  updateSessionCustomer,
  type AuthSession,
} from '../lib/auth-storage'
import type { LoginRequest } from '../types/api'
import { AuthContext, type AuthStatus } from './auth-context'

export function AuthProvider({ children }: PropsWithChildren) {
  const [session, setSession] = useState<AuthSession | null>(() => readSession())
  const [status, setStatus] = useState<AuthStatus>(session ? 'checking' : 'anonymous')

  const logout = useCallback(() => {
    clearSession()
    setSession(null)
    setStatus('anonymous')
  }, [])

  useEffect(() => {
    if (!session || status !== 'checking') return

    const validateSession = async () => {
      try {
        const customer = await customerApi.me(session.accessToken)
        setSession(updateSessionCustomer(session, customer))
        setStatus('authenticated')
      } catch {
        clearSession()
        setSession(null)
        setStatus('anonymous')
      }
    }

    void validateSession()
  }, [session, status])

  useEffect(() => {
    if (!session || status !== 'authenticated') return

    const remainingMilliseconds = Date.parse(session.expiresAt) - Date.now()
    const maximumTimeout = 2_147_483_647
    const timeout = window.setTimeout(
      logout,
      Math.min(Math.max(remainingMilliseconds, 0), maximumTimeout),
    )
    return () => window.clearTimeout(timeout)
  }, [logout, session, status])

  const login = useCallback(async (credentials: LoginRequest) => {
    const auth = await customerApi.login(credentials)
    setSession(saveSession(auth))
    setStatus('authenticated')
  }, [])

  const value = useMemo(
    () => ({ status, session, login, logout }),
    [status, session, login, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
