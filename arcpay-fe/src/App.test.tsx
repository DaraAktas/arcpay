import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
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
      return Promise.resolve(url.endsWith('/api/wallet') || url.endsWith('/api/transaction')
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
      Promise.resolve(
        input.toString().endsWith('/api/wallet') || input.toString().endsWith('/api/transaction')
          ? jsonResponse([])
          : jsonResponse(customer),
      ),
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
      if (url.endsWith('/api/customer/resolve-recipient')) {
        return Promise.resolve(jsonResponse({ customerNumber: 'ARC-1000000002', displayName: 'Grace Hopper' }))
      }
      if (url.endsWith('/api/transaction')) return Promise.resolve(jsonResponse([]))
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

  it('transfers money and shows the completed movement', async () => {
    sessionStorage.setItem(
      'arcpay.session',
      JSON.stringify({ accessToken: 'stored.token', expiresAt: '2099-01-01T00:00:00Z', customer }),
    )
    const wallet = { id: 1, customerNumber: customer.customerNumber, balance: 500, currency: 'TRY' }
    let transferCompleted = false
    const transfer = {
      transactionRef: '22222222-2222-2222-2222-222222222222',
      receiverCustomerNumber: 'ARC-1000000002',
      amount: 125,
      currency: 'TRY',
      senderWallet: { ...wallet, balance: 375 },
      createdAt: '2026-07-27T10:00:00Z',
      isReplay: false,
    }
    const history = {
      transactionRef: transfer.transactionRef,
      type: 'Transfer',
      direction: 'Outgoing',
      amount: 125,
      currency: 'TRY',
      status: 'Completed',
      counterpartyCustomerNumber: transfer.receiverCustomerNumber,
      description: 'Yemek payı',
      createdAt: transfer.createdAt,
    }
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL, options?: RequestInit) => {
      const url = input.toString()
      if (url.endsWith('/api/customer/me')) return Promise.resolve(jsonResponse(customer))
      if (url.endsWith('/api/wallet')) return Promise.resolve(jsonResponse([wallet]))
      if (url.endsWith('/api/customer/resolve-recipient')) {
        return Promise.resolve(jsonResponse({ customerNumber: 'ARC-1000000002', displayName: 'Grace Hopper' }))
      }
      if (url.endsWith('/api/transaction/transfer') && options?.method === 'POST') {
        transferCompleted = true
        return Promise.resolve(jsonResponse(transfer, 201))
      }
      if (url.endsWith('/api/transaction')) {
        return Promise.resolve(jsonResponse(transferCompleted ? [history] : []))
      }
      throw new Error(`Unexpected request: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    render(
      <MemoryRouter initialEntries={['/hesabim']}>
        <App />
      </MemoryRouter>,
    )

    expect(await screen.findByText('₺500,00')).toBeVisible()
    fireEvent.click(screen.getByRole('button', { name: 'Para gönder' }))
    fireEvent.change(screen.getByLabelText('Alıcı bilgisi'), {
      target: { value: 'grace@arcpay.test' },
    })
    fireEvent.change(screen.getByLabelText('Tutar'), { target: { value: '125' } })
    fireEvent.change(screen.getByLabelText(/Açıklama/), { target: { value: 'Yemek payı' } })
    fireEvent.click(screen.getByRole('button', { name: 'Transferi tamamla' }))

    expect(await screen.findByText(/Grace Hopper adlı alıcıya/)).toHaveTextContent('₺125,00 gönderildi.')
    expect(screen.getByText('Giden transfer')).toBeVisible()
    expect(screen.getByText('ARC-1000000002')).toBeVisible()
    expect(screen.getByText('-₺125,00')).toBeVisible()
  })

  it('shows an unknown recipient error inside the transfer dialog', async () => {
    sessionStorage.setItem(
      'arcpay.session',
      JSON.stringify({ accessToken: 'stored.token', expiresAt: '2099-01-01T00:00:00Z', customer }),
    )
    const wallet = { id: 1, customerNumber: customer.customerNumber, balance: 500, currency: 'TRY' }
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = input.toString()
      if (url.endsWith('/api/customer/me')) return Promise.resolve(jsonResponse(customer))
      if (url.endsWith('/api/wallet')) return Promise.resolve(jsonResponse([wallet]))
      if (url.endsWith('/api/transaction')) return Promise.resolve(jsonResponse([]))
      if (url.endsWith('/api/customer/resolve-recipient')) {
        return Promise.resolve(jsonResponse({ title: 'Customer was not found.', code: 'Customer.NotFound' }, 404))
      }
      throw new Error(`Unexpected request: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    render(<MemoryRouter initialEntries={['/hesabim']}><App /></MemoryRouter>)

    expect(await screen.findByText('₺500,00')).toBeVisible()
    fireEvent.click(screen.getByRole('button', { name: 'Para gönder' }))
    fireEvent.change(screen.getByLabelText('Alıcı bilgisi'), { target: { value: 'yanlis@arcpay.test' } })
    fireEvent.change(screen.getByLabelText('Tutar'), { target: { value: '10' } })
    fireEvent.click(screen.getByRole('button', { name: 'Transferi tamamla' }))

    const dialog = screen.getByRole('dialog', { name: 'Para gönderin' })
    expect(await screen.findByText(/eşleşen alıcı bulunamadı/)).toBeVisible()
    expect(dialog).toContainElement(screen.getByRole('alert'))
  })

  it('closes a zero-balance wallet', async () => {
    sessionStorage.setItem(
      'arcpay.session',
      JSON.stringify({ accessToken: 'stored.token', expiresAt: '2099-01-01T00:00:00Z', customer }),
    )
    const wallet = { id: 1, customerNumber: customer.customerNumber, balance: 0, currency: 'TRY' }
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL, options?: RequestInit) => {
      const url = input.toString()
      if (url.endsWith('/api/customer/me')) return Promise.resolve(jsonResponse(customer))
      if (url.endsWith('/api/wallet') && options?.method === 'GET') return Promise.resolve(jsonResponse([wallet]))
      if (url.endsWith('/api/transaction')) return Promise.resolve(jsonResponse([]))
      if (url.endsWith('/api/wallet/TRY') && options?.method === 'DELETE') return Promise.resolve(new Response(null, { status: 204 }))
      throw new Error(`Unexpected request: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    render(<MemoryRouter initialEntries={['/hesabim']}><App /></MemoryRouter>)

    expect(await screen.findByText('₺0,00')).toBeVisible()
    fireEvent.click(screen.getByRole('button', { name: 'Cüzdanı kapat' }))
    fireEvent.click(screen.getByRole('dialog', { name: 'Cüzdanı kapatın' }).querySelector('.danger-button')!)

    expect(await screen.findByText(/cüzdanınız kapatıldı/)).toBeVisible()
    expect(screen.getByText('0 aktif cüzdan')).toBeVisible()
  })

  it('purchases a market asset and shows it in the portfolio', async () => {
    sessionStorage.setItem(
      'arcpay.session',
      JSON.stringify({ accessToken: 'stored.token', expiresAt: '2099-01-01T00:00:00Z', customer }),
    )
    const quote = { symbol: 'AAPL', name: 'Apple', price: 100, currency: 'USD', changePercent: 1.2, asOf: '2026-07-29T00:00:00Z', source: 'Demo' }
    let purchased = false
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = input.toString()
      if (url.endsWith('/api/customer/me')) return Promise.resolve(jsonResponse(customer))
      if (url.endsWith('/api/investment/market')) return Promise.resolve(jsonResponse([quote]))
      if (url.endsWith('/api/investment/portfolio')) return Promise.resolve(jsonResponse({
        customerNumber: customer.customerNumber,
        holdings: purchased ? [{ symbol: 'AAPL', quantity: 2, averageCost: 100, currency: 'USD' }] : [],
      }))
      if (url.endsWith('/api/investment/purchase')) {
        purchased = true
        return Promise.resolve(jsonResponse({ purchaseRef: crypto.randomUUID(), symbol: 'AAPL', quantity: 2, unitPrice: 100, totalAmount: 200, currency: 'USD', status: 'Completed', isReplay: false }))
      }
      throw new Error(`Unexpected request: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    render(<MemoryRouter initialEntries={['/yatirimlar']}><App /></MemoryRouter>)

    const marketCard = (await screen.findByText('AAPL')).closest('article')!
    fireEvent.click(within(marketCard).getByRole('button', { name: 'Satın al' }))
    fireEvent.change(screen.getByLabelText('Adet'), { target: { value: '2' } })
    fireEvent.click(screen.getByRole('dialog', { name: 'Varlık satın alın' }).querySelector('.primary-button')!)

    expect(await screen.findByText(/2 AAPL/)).toBeVisible()
    expect(screen.getByText('2 adet')).toBeVisible()
  })

  it('shows that a failed portfolio write was automatically refunded', async () => {
    sessionStorage.setItem(
      'arcpay.session',
      JSON.stringify({ accessToken: 'stored.token', expiresAt: '2099-01-01T00:00:00Z', customer }),
    )
    const quote = { symbol: 'AAPL', name: 'Apple', price: 100, currency: 'USD', changePercent: 1.2, asOf: '2026-07-29T00:00:00Z', source: 'Demo' }
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = input.toString()
      if (url.endsWith('/api/customer/me')) return Promise.resolve(jsonResponse(customer))
      if (url.endsWith('/api/investment/market')) return Promise.resolve(jsonResponse([quote]))
      if (url.endsWith('/api/investment/portfolio')) return Promise.resolve(jsonResponse({ customerNumber: customer.customerNumber, holdings: [] }))
      if (url.endsWith('/api/investment/purchase')) return Promise.resolve(jsonResponse({ title: 'Purchase refunded.', code: 'investment.purchase_compensated' }, 409))
      throw new Error(`Unexpected request: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    render(<MemoryRouter initialEntries={['/yatirimlar']}><App /></MemoryRouter>)

    const marketCard = (await screen.findByText('AAPL')).closest('article')!
    fireEvent.click(within(marketCard).getByRole('button', { name: 'Satın al' }))
    fireEvent.click(screen.getByLabelText(/Telafi senaryosunu test et/))
    fireEvent.click(screen.getByRole('dialog', { name: 'Varlık satın alın' }).querySelector('.primary-button')!)

    expect(await screen.findByText(/otomatik olarak iade edildi/)).toBeVisible()
  })
})
