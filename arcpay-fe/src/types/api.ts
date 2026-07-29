export interface Customer {
  customerNumber: string
  fullName: string
  email: string
  phoneNumber?: string | null
}

export interface RegisterRequest {
  fullName: string
  email: string
  phoneNumber: string
  password: string
}

export interface LoginRequest {
  email: string
  password: string
}

export interface AuthResponse {
  accessToken: string
  tokenType: string
  expiresAt: string
  customer: Customer
}

export interface RecipientLookup {
  customerNumber: string
  displayName: string
}

export interface Wallet {
  id: number
  customerNumber: string
  balance: number
  currency: string
}

export interface DepositResponse {
  transactionRef: string
  wallet: Wallet
}

export interface TransferResponse {
  transactionRef: string
  receiverCustomerNumber: string
  amount: number
  currency: string
  senderWallet: Wallet
  createdAt: string
  isReplay: boolean
}

export interface TransactionHistory {
  transactionRef: string
  type: 'Deposit' | 'Transfer' | string
  direction: 'Incoming' | 'Outgoing'
  amount: number
  currency: string
  status: 'Completed' | 'Pending' | 'Failed' | string
  counterpartyCustomerNumber?: string | null
  description?: string | null
  createdAt: string
}

export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  code?: string
  correlationId?: string
  errors?: Record<string, string[]>
}

export interface MarketQuote {
  symbol: string
  name: string
  price: number
  currency: string
  changePercent: number
  asOf: string
  source: string
}

export interface Holding {
  symbol: string
  quantity: number
  averageCost: number
  currency: string
}

export interface Portfolio {
  customerNumber: string
  holdings: Holding[]
}

export interface InvestmentPurchase {
  purchaseRef: string
  symbol: string
  quantity: number
  unitPrice: number
  totalAmount: number
  currency: string
  status: string
  isReplay: boolean
}
