export interface Customer {
  customerNumber: string
  fullName: string
  email: string
}

export interface RegisterRequest {
  fullName: string
  email: string
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

export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  code?: string
  correlationId?: string
  errors?: Record<string, string[]>
}
