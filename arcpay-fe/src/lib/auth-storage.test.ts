import { beforeEach, describe, expect, it, vi } from 'vitest'
import { clearSession, readSession, saveSession, updateSessionCustomer } from './auth-storage'

const authResponse = {
  accessToken: 'valid.jwt.token',
  tokenType: 'Bearer',
  expiresAt: '2030-01-01T12:00:00Z',
  customer: {
    customerNumber: 'ARC-1000000001',
    fullName: 'Ada Lovelace',
    email: 'ada@arcpay.test',
  },
}

describe('auth storage', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2029-12-01T12:00:00Z'))
  })

  it('stores and restores a valid session', () => {
    saveSession(authResponse)

    expect(readSession()).toEqual({
      accessToken: authResponse.accessToken,
      expiresAt: authResponse.expiresAt,
      customer: authResponse.customer,
    })
  })

  it('removes an expired session', () => {
    vi.setSystemTime(new Date('2030-02-01T12:00:00Z'))
    saveSession(authResponse)

    expect(readSession()).toBeNull()
    expect(sessionStorage.length).toBe(0)
  })

  it('updates customer data without replacing the token', () => {
    const session = saveSession(authResponse)
    const updated = updateSessionCustomer(session, {
      ...authResponse.customer,
      fullName: 'Ada Byron',
    })

    expect(updated.accessToken).toBe(authResponse.accessToken)
    expect(readSession()?.customer.fullName).toBe('Ada Byron')
  })

  it('clears a session explicitly', () => {
    saveSession(authResponse)
    clearSession()
    expect(readSession()).toBeNull()
  })
})
