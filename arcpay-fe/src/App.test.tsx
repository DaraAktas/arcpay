import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import App from './App'

const customer = {
  customerNumber: 'ARC-1000000001',
  fullName: 'Ada Lovelace',
  email: 'ada@arcpay.test',
}

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('ArcPay authentication flow', () => {
  it('redirects an anonymous visitor from the account page to login', async () => {
    render(
      <MemoryRouter initialEntries={['/hesabim']}>
        <App />
      </MemoryRouter>,
    )

    expect(await screen.findByRole('heading', { name: 'Hesabınıza giriş yapın' })).toBeVisible()
  })

  it('logs in and opens the protected account page', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      jsonResponse({
        accessToken: 'valid.jwt.token',
        tokenType: 'Bearer',
        expiresAt: '2099-01-01T00:00:00Z',
        customer,
      }),
    )
    vi.stubGlobal('fetch', fetchMock)

    render(
      <MemoryRouter initialEntries={['/giris']}>
        <App />
      </MemoryRouter>,
    )

    fireEvent.change(screen.getByLabelText('E-posta adresi'), {
      target: { value: customer.email },
    })
    fireEvent.change(screen.getByLabelText('Parola'), {
      target: { value: 'SecurePass1' },
    })
    fireEvent.click(screen.getByRole('button', { name: 'Giriş yap' }))

    expect(await screen.findByRole('heading', { name: 'Merhaba, Ada.' })).toBeVisible()
    expect(screen.getByText(customer.customerNumber)).toBeVisible()
    expect(fetchMock).toHaveBeenCalledWith(
      'http://localhost:5000/api/customer/login',
      expect.objectContaining({ method: 'POST' }),
    )
  })

  it('keeps registration client-side when passwords do not match', async () => {
    const fetchMock = vi.fn()
    vi.stubGlobal('fetch', fetchMock)

    render(
      <MemoryRouter initialEntries={['/kayit']}>
        <App />
      </MemoryRouter>,
    )

    fireEvent.change(screen.getByLabelText('Ad soyad'), { target: { value: 'Ada Lovelace' } })
    fireEvent.change(screen.getByLabelText('E-posta adresi'), {
      target: { value: customer.email },
    })
    fireEvent.change(screen.getByLabelText('Parola'), { target: { value: 'SecurePass1' } })
    fireEvent.change(screen.getByLabelText('Parola tekrarı'), {
      target: { value: 'DifferentPass1' },
    })
    fireEvent.click(screen.getByRole('button', { name: 'Hesabımı oluştur' }))

    expect(await screen.findByText('Parolalar eşleşmiyor.')).toBeVisible()
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('restores a stored session only after the API validates it', async () => {
    sessionStorage.setItem(
      'arcpay.session',
      JSON.stringify({ accessToken: 'stored.token', expiresAt: '2099-01-01T00:00:00Z', customer }),
    )
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse(customer))
    vi.stubGlobal('fetch', fetchMock)

    render(
      <MemoryRouter initialEntries={['/hesabim']}>
        <App />
      </MemoryRouter>,
    )

    expect(screen.getByText('Güvenli oturum kontrol ediliyor…')).toBeVisible()
    expect(await screen.findByRole('heading', { name: 'Merhaba, Ada.' })).toBeVisible()
    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(
        'http://localhost:5000/api/customer/me',
        expect.objectContaining({ method: 'GET' }),
      )
    })
  })
})
