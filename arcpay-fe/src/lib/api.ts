import type {
  AuthResponse,
  Customer,
  LoginRequest,
  ProblemDetails,
  RegisterRequest,
  Wallet,
  DepositResponse,
  TransactionHistory,
  TransferResponse,
  RecipientLookup,
  MarketQuote,
  Portfolio,
  InvestmentPurchase,
} from '../types/api'

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000').replace(
  /\/$/,
  '',
)

export class ApiError extends Error {
  readonly status: number
  readonly problem: ProblemDetails

  constructor(status: number, problem: ProblemDetails) {
    super(problem.title ?? 'İstek tamamlanamadı.')
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }
}

async function request<T>(
  path: string,
  options: RequestInit = {},
  accessToken?: string,
): Promise<T> {
  const headers = new Headers(options.headers)
  headers.set('Accept', 'application/json')

  if (options.body) {
    headers.set('Content-Type', 'application/json')
  }

  if (accessToken) {
    headers.set('Authorization', `Bearer ${accessToken}`)
  }

  let response: Response
  try {
    response = await fetch(`${API_BASE_URL}${path}`, { ...options, headers })
  } catch {
    throw new ApiError(0, {
      title: 'ArcPay servislerine ulaşılamıyor. Lütfen bağlantınızı kontrol edin.',
    })
  }

  if (!response.ok) {
    const contentType = response.headers.get('content-type') ?? ''
    const problem = contentType.includes('json')
      ? ((await response.json()) as ProblemDetails)
      : { title: 'İstek tamamlanamadı.' }
    throw new ApiError(response.status, problem)
  }

  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}

export const customerApi = {
  register: (payload: RegisterRequest) =>
    request<Customer>('/api/customer/register', {
      method: 'POST',
      body: JSON.stringify(payload),
    }),

  login: (payload: LoginRequest) =>
    request<AuthResponse>('/api/customer/login', {
      method: 'POST',
      body: JSON.stringify(payload),
    }),

  me: (accessToken: string) =>
    request<Customer>('/api/customer/me', { method: 'GET' }, accessToken),

  resolveRecipient: (identifier: string, accessToken: string) =>
    request<RecipientLookup>(
      '/api/customer/resolve-recipient',
      { method: 'POST', body: JSON.stringify({ identifier }) },
      accessToken,
    ),
}

export const walletApi = {
  list: (accessToken: string) =>
    request<Wallet[]>('/api/wallet', { method: 'GET' }, accessToken),

  open: (currency: string, accessToken: string) =>
    request<Wallet>(
      '/api/wallet',
      { method: 'POST', body: JSON.stringify({ currency }) },
      accessToken,
    ),

  deposit: (currency: string, amount: number, transactionRef: string, accessToken: string) =>
    request<DepositResponse>(
      `/api/wallet/${encodeURIComponent(currency)}/deposit`,
      { method: 'POST', body: JSON.stringify({ amount, transactionRef }) },
      accessToken,
    ),

  close: (currency: string, accessToken: string) =>
    request<void>(
      `/api/wallet/${encodeURIComponent(currency)}`,
      { method: 'DELETE' },
      accessToken,
    ),
}

export const transactionApi = {
  list: (accessToken: string) =>
    request<TransactionHistory[]>('/api/transaction', { method: 'GET' }, accessToken),

  transfer: (
    toCustomerNumber: string,
    amount: number,
    currency: string,
    transactionRef: string,
    description: string,
    accessToken: string,
  ) => request<TransferResponse>(
    '/api/transaction/transfer',
    {
      method: 'POST',
      body: JSON.stringify({ toCustomerNumber, amount, currency, transactionRef, description }),
    },
    accessToken,
  ),
}

export const investmentApi = {
  market: (accessToken: string) =>
    request<MarketQuote[]>('/api/investment/market', { method: 'GET' }, accessToken),

  portfolio: (accessToken: string) =>
    request<Portfolio>('/api/investment/portfolio', { method: 'GET' }, accessToken),

  purchase: (
    symbol: string,
    quantity: number,
    purchaseRef: string,
    simulatePortfolioFailure: boolean,
    accessToken: string,
  ) => request<InvestmentPurchase>(
    '/api/investment/purchase',
    {
      method: 'POST',
      body: JSON.stringify({ symbol, quantity, purchaseRef, simulatePortfolioFailure }),
    },
    accessToken,
  ),
}
