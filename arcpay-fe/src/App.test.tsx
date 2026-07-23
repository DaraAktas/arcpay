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
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = input.toString()
      return Promise.resolve(url.endsWith('/api/wallet')
        ? jsonResponse([])
        : jsonResponse({
            accessToken: 'valid.jwt.token',
            tokenType: 'Bearer',
            expiresAt: '2099-01-01T00:00:00Z',
            customer,
          }))
    })
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
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) =>
      Promise.resolve(input.toString().endsWith('/api/wallet') ? jsonResponse([]) : jsonResponse(customer)),
    )
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

  it('opens a TRY wallet and deposits money', async () => {
    sessionStorage.setItem(
      'arcpay.session',
      JSON.stringify({ accessToken: 'stored.token', expiresAt: '2099-01-01T00:00:00Z', customer }),
    )
    const wallet = { id: 1, customerNumber: customer.customerNumber, balance: 0, currency: 'TRY' }
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL, options?: RequestInit) => {
      const url = input.toString()
      if (url.endsWith('/api/customer/me')) return Promise.resolve(jsonResponse(customer))
      if (url.endsWith('/api/wallet') && options?.method === 'GET') return Promise.resolve(jsonResponse([]))
      if (url.endsWith('/api/wallet') && options?.method === 'POST') return Promise.resolve(jsonResponse(wallet, 201))
      if (url.endsWith('/api/wallet/TRY/deposit')) {
        return Promise.resolve(jsonResponse({
          transactionRef: '11111111-1111-1111-1111-111111111111',
          wallet: { ...wallet, balance: 1250.5 },
        }))
      }
      throw new Error(`Unexpected request: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    render(
      <MemoryRouter initialEntries={['/hesabim']}>
        <App />
      </MemoryRouter>,
    )

    expect(await screen.findByRole('heading', { name: 'Merhaba, Ada.' })).toBeVisible()
    await waitFor(() => expect(screen.getByText('0 aktif cüzdan')).toBeVisible())
    fireEvent.click(screen.getByRole('button', { name: 'Cüzdan aç' }))

    expect(await screen.findByText('TRY cüzdanınız açıldı.')).toBeVisible()
    fireEvent.click(screen.getByRole('button', { name: 'Para yatır' }))
    fireEvent.change(screen.getByLabelText('Tutar'), { target: { value: '1250,50' } })
    fireEvent.click(screen.getByRole('button', { name: 'Bakiyeye ekle' }))

    expect(await screen.findByText(/cüzdanınıza/)).toHaveTextContent('₺1.250,50 yatırıldı.')
    expect(fetchMock).toHaveBeenCalledWith(
      'http://localhost:5000/api/wallet/TRY/deposit',
      expect.objectContaining({ method: 'POST' }),
    )
  })
})
