import type { AuthResponse, Customer } from '../types/api'

const STORAGE_KEY = 'arcpay.session'

export interface AuthSession {
  accessToken: string
  expiresAt: string
  customer: Customer
}

function isCustomer(value: unknown): value is Customer {
  if (!value || typeof value !== 'object') return false
  const customer = value as Record<string, unknown>
  return (
    typeof customer.customerNumber === 'string' &&
    typeof customer.fullName === 'string' &&
    typeof customer.email === 'string'
  )
}

function isSession(value: unknown): value is AuthSession {
  if (!value || typeof value !== 'object') return false
  const session = value as Record<string, unknown>
  return (
    typeof session.accessToken === 'string' &&
    typeof session.expiresAt === 'string' &&
    isCustomer(session.customer)
  )
}

export function saveSession(auth: AuthResponse): AuthSession {
  const session: AuthSession = {
    accessToken: auth.accessToken,
    expiresAt: auth.expiresAt,
    customer: auth.customer,
  }
  sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session))
  return session
}

export function readSession(): AuthSession | null {
  const serialized = sessionStorage.getItem(STORAGE_KEY)
  if (!serialized) return null

  try {
    const session: unknown = JSON.parse(serialized)
    if (!isSession(session) || Date.parse(session.expiresAt) <= Date.now()) {
      clearSession()
      return null
    }
    return session
  } catch {
    clearSession()
    return null
  }
}

export function clearSession(): void {
  sessionStorage.removeItem(STORAGE_KEY)
}

export function updateSessionCustomer(session: AuthSession, customer: Customer): AuthSession {
  const updated = { ...session, customer }
  sessionStorage.setItem(STORAGE_KEY, JSON.stringify(updated))
  return updated
}
