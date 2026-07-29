import { useState, type FormEvent } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { AuthLayout } from '../components/AuthLayout'
import { FormField } from '../components/FormField'
import { useAuth } from '../auth/auth-context'
import { getErrorMessage } from '../lib/form-errors'

interface LoginLocationState {
  from?: string
  email?: string
  registered?: boolean
}

export function LoginPage() {
  const { login, status } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const state = (location.state ?? {}) as LoginLocationState
  const [email, setEmail] = useState(state.email ?? '')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const useDemoAccount = (demoEmail: string) => {
    setEmail(demoEmail)
    setPassword('Demo123!')
    setError('')
  }

  if (status === 'authenticated') return <Navigate to="/hesabim" replace />

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError('')
    setIsSubmitting(true)

    try {
      await login({ email, password })
      navigate(state.from ?? '/hesabim', { replace: true })
    } catch (caught) {
      setError(getErrorMessage(caught))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <AuthLayout
      eyebrow="Tekrar hoş geldiniz"
      title="Hesabınıza giriş yapın"
      description="Finansal yolculuğunuza kaldığınız yerden devam edin."
    >
      {state.registered && (
        <div className="notice notice-success" role="status">
          Hesabınız hazır. Şimdi giriş yapabilirsiniz.
        </div>
      )}

      {error && <div className="notice notice-error" role="alert">{error}</div>}

      <section className="demo-accounts" aria-label="Demo hesaplar">
        <strong>Demo hesaplar</strong>
        <p>Parola: <code>Demo123!</code></p>
        <div className="demo-account-actions">
          <button type="button" onClick={() => useDemoAccount('demo.sender@arcpay.test')}>Gönderen · 1.000 TRY</button>
          <button type="button" onClick={() => useDemoAccount('demo.receiver@arcpay.test')}>Alıcı · 250 TRY</button>
          <button type="button" onClick={() => useDemoAccount('demo.empty@arcpay.test')}>Boş · 0 TRY</button>
        </div>
      </section>

      <form className="auth-form" onSubmit={handleSubmit} noValidate>
        <FormField
          label="E-posta adresi"
          type="email"
          name="email"
          autoComplete="email"
          placeholder="ornek@eposta.com"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          required
        />
        <FormField
          label="Parola"
          type="password"
          name="password"
          autoComplete="current-password"
          placeholder="Parolanızı girin"
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          minLength={8}
          required
        />
        <button className="primary-button" type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Giriş yapılıyor…' : 'Giriş yap'}
          {!isSubmitting && <span aria-hidden="true">→</span>}
        </button>
      </form>

      <p className="form-switch">
        ArcPay’e yeni misiniz? <Link to="/kayit">Ücretsiz hesap oluşturun</Link>
      </p>
    </AuthLayout>
  )
}
