import { useState, type FormEvent } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/auth-context'
import { AuthLayout } from '../components/AuthLayout'
import { FormField } from '../components/FormField'
import { customerApi } from '../lib/api'
import { getErrorMessage } from '../lib/form-errors'

interface RegisterForm {
  fullName: string
  email: string
  password: string
  confirmPassword: string
}

const emptyForm: RegisterForm = {
  fullName: '',
  email: '',
  password: '',
  confirmPassword: '',
}

export function RegisterPage() {
  const { status } = useAuth()
  const navigate = useNavigate()
  const [form, setForm] = useState(emptyForm)
  const [error, setError] = useState('')
  const [fieldErrors, setFieldErrors] = useState<Partial<Record<keyof RegisterForm, string>>>({})
  const [isSubmitting, setIsSubmitting] = useState(false)

  if (status === 'authenticated') return <Navigate to="/hesabim" replace />

  const updateField = (field: keyof RegisterForm, value: string) => {
    setForm((current) => ({ ...current, [field]: value }))
    setFieldErrors((current) => ({ ...current, [field]: undefined }))
  }

  const validate = () => {
    const errors: Partial<Record<keyof RegisterForm, string>> = {}
    if (!form.fullName.trim()) errors.fullName = 'Ad soyad gereklidir.'
    if (!/^\S+@\S+\.\S+$/.test(form.email)) errors.email = 'Geçerli bir e-posta adresi girin.'
    if (form.password.length < 8) errors.password = 'Parola en az 8 karakter olmalıdır.'
    else if (!/[A-Z]/.test(form.password) || !/[a-z]/.test(form.password) || !/\d/.test(form.password)) {
      errors.password = 'Parola büyük harf, küçük harf ve rakam içermelidir.'
    }
    if (form.password !== form.confirmPassword) errors.confirmPassword = 'Parolalar eşleşmiyor.'
    setFieldErrors(errors)
    return Object.keys(errors).length === 0
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError('')
    if (!validate()) return

    setIsSubmitting(true)
    try {
      await customerApi.register({
        fullName: form.fullName,
        email: form.email,
        password: form.password,
      })
      navigate('/giris', {
        replace: true,
        state: { registered: true, email: form.email },
      })
    } catch (caught) {
      setError(getErrorMessage(caught))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <AuthLayout
      eyebrow="ArcPay’e katılın"
      title="Finansal rotanızı çizin"
      description="Dakikalar içinde hesabınızı oluşturun ve kontrolü elinize alın."
    >
      {error && <div className="notice notice-error" role="alert">{error}</div>}

      <form className="auth-form compact-form" onSubmit={handleSubmit} noValidate>
        <FormField
          label="Ad soyad"
          name="fullName"
          autoComplete="name"
          placeholder="Adınız ve soyadınız"
          value={form.fullName}
          onChange={(event) => updateField('fullName', event.target.value)}
          error={fieldErrors.fullName}
          maxLength={150}
          required
        />
        <FormField
          label="E-posta adresi"
          type="email"
          name="email"
          autoComplete="email"
          placeholder="ornek@eposta.com"
          value={form.email}
          onChange={(event) => updateField('email', event.target.value)}
          error={fieldErrors.email}
          maxLength={320}
          required
        />
        <div className="field-grid">
          <FormField
            label="Parola"
            type="password"
            name="password"
            autoComplete="new-password"
            placeholder="En az 8 karakter"
            value={form.password}
            onChange={(event) => updateField('password', event.target.value)}
            error={fieldErrors.password}
            maxLength={72}
            required
          />
          <FormField
            label="Parola tekrarı"
            type="password"
            name="confirmPassword"
            autoComplete="new-password"
            placeholder="Parolanızı tekrarlayın"
            value={form.confirmPassword}
            onChange={(event) => updateField('confirmPassword', event.target.value)}
            error={fieldErrors.confirmPassword}
            maxLength={72}
            required
          />
        </div>
        <p className="password-hint">8–72 karakter · büyük/küçük harf · en az bir rakam</p>
        <button className="primary-button" type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Hesap oluşturuluyor…' : 'Hesabımı oluştur'}
          {!isSubmitting && <span aria-hidden="true">→</span>}
        </button>
      </form>

      <p className="form-switch">
        Zaten hesabınız var mı? <Link to="/giris">Giriş yapın</Link>
      </p>
    </AuthLayout>
  )
}
