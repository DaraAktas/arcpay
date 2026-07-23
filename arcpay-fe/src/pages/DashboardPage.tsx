import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { useAuth } from '../auth/auth-context'
import { Brand } from '../components/Brand'
import { ApiError, walletApi } from '../lib/api'
import { getErrorMessage } from '../lib/form-errors'
import type { Wallet } from '../types/api'

const supportedCurrencies = ['TRY', 'USD', 'EUR', 'XAU'] as const

const currencyMeta: Record<string, { symbol: string; name: string }> = {
  TRY: { symbol: '₺', name: 'Türk lirası' },
  USD: { symbol: '$', name: 'Amerikan doları' },
  EUR: { symbol: '€', name: 'Euro' },
  XAU: { symbol: 'Au', name: 'Altın' },
}

function initials(fullName: string): string {
  return fullName
    .split(/\s+/)
    .slice(0, 2)
    .map((part) => part[0])
    .join('')
    .toLocaleUpperCase('tr-TR')
}

function formatBalance(wallet: Wallet): string {
  if (wallet.currency === 'XAU') {
    return `${new Intl.NumberFormat('tr-TR', { maximumFractionDigits: 8 }).format(wallet.balance)} XAU`
  }

  return new Intl.NumberFormat('tr-TR', {
    style: 'currency',
    currency: wallet.currency,
    minimumFractionDigits: 2,
    maximumFractionDigits: 8,
  }).format(wallet.balance)
}

export function DashboardPage() {
  const { session, logout } = useAuth()
  const [wallets, setWallets] = useState<Wallet[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isOpening, setIsOpening] = useState(false)
  const [selectedCurrency, setSelectedCurrency] = useState('TRY')
  const [depositWallet, setDepositWallet] = useState<Wallet | null>(null)
  const [depositAmount, setDepositAmount] = useState('')
  const [isDepositing, setIsDepositing] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')

  useEffect(() => {
    if (!session) return
    let active = true

    const loadWallets = async () => {
      try {
        const result = await walletApi.list(session.accessToken)
        if (active) setWallets(result)
      } catch (caught) {
        if (caught instanceof ApiError && caught.status === 401) {
          logout()
          return
        }
        if (active) setError(getErrorMessage(caught))
      } finally {
        if (active) setIsLoading(false)
      }
    }

    void loadWallets()
    return () => {
      active = false
    }
  }, [logout, session])

  const unopenedCurrencies = useMemo(
    () => supportedCurrencies.filter((code) => !wallets.some((wallet) => wallet.currency === code)),
    [wallets],
  )
  const currencyToOpen = unopenedCurrencies.find((currency) => currency === selectedCurrency)
    ?? unopenedCurrencies[0]
    ?? ''

  if (!session) return null
  const { customer } = session

  const handleOpenWallet = async () => {
    setError('')
    setNotice('')
    setIsOpening(true)
    try {
      const wallet = await walletApi.open(currencyToOpen, session.accessToken)
      setWallets((current) => [...current, wallet].sort((a, b) => a.currency.localeCompare(b.currency)))
      setNotice(`${wallet.currency} cüzdanınız açıldı.`)
    } catch (caught) {
      setError(getErrorMessage(caught))
    } finally {
      setIsOpening(false)
    }
  }

  const handleDeposit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!depositWallet) return

    const amount = Number(depositAmount.replace(',', '.'))
    if (!Number.isFinite(amount) || amount <= 0) {
      setError('Yatırılacak tutar sıfırdan büyük olmalıdır.')
      return
    }

    setError('')
    setNotice('')
    setIsDepositing(true)
    try {
      const result = await walletApi.deposit(
        depositWallet.currency,
        amount,
        crypto.randomUUID(),
        session.accessToken,
      )
      setWallets((current) =>
        current.map((wallet) => (wallet.id === result.wallet.id ? result.wallet : wallet)),
      )
      setNotice(`${result.wallet.currency} cüzdanınıza ${formatBalance({ ...result.wallet, balance: amount })} yatırıldı.`)
      setDepositWallet(null)
      setDepositAmount('')
    } catch (caught) {
      setError(getErrorMessage(caught))
    } finally {
      setIsDepositing(false)
    }
  }

  return (
    <div className="dashboard-shell">
      <header className="dashboard-header">
        <Brand />
        <div className="header-actions">
          <span className="session-indicator"><i /> Güvenli oturum</span>
          <button className="text-button" type="button" onClick={logout}>Çıkış yap</button>
        </div>
      </header>

      <main className="dashboard-main">
        <section className="welcome-row">
          <div>
            <p className="eyebrow">Cüzdanlarım</p>
            <h1>Merhaba, {customer.fullName.split(' ')[0]}.</h1>
            <p>Bakiyelerinizi tek ekrandan yönetin ve yeni para birimleri için cüzdan açın.</p>
          </div>
          <div className="phase-badge"><span>✓</span> Faz 3 · DDD çekirdeği</div>
        </section>

        {(error || notice) && (
          <div className={`dashboard-notice ${error ? 'is-error' : ''}`} role={error ? 'alert' : 'status'}>
            {error || notice}
            <button type="button" aria-label="Bildirimi kapat" onClick={() => { setError(''); setNotice('') }}>×</button>
          </div>
        )}

        <section className="wallet-section" aria-labelledby="wallets-title">
          <div className="section-heading">
            <div>
              <span className="card-label">Portföy görünümü</span>
              <h2 id="wallets-title">Cüzdanlarınız</h2>
            </div>
            <span className="wallet-count">{wallets.length} aktif cüzdan</span>
          </div>

          {isLoading ? (
            <div className="wallet-loading" aria-live="polite">Cüzdanlar yükleniyor…</div>
          ) : (
            <div className="wallet-grid">
              {wallets.map((wallet) => (
                <article className={`wallet-card wallet-${wallet.currency.toLowerCase()}`} key={wallet.id}>
                  <div className="wallet-card-header">
                    <span className="currency-symbol">{currencyMeta[wallet.currency]?.symbol ?? wallet.currency}</span>
                    <div>
                      <strong>{wallet.currency}</strong>
                      <span>{currencyMeta[wallet.currency]?.name}</span>
                    </div>
                    <i className="active-dot" aria-label="Aktif" />
                  </div>
                  <div className="wallet-balance">
                    <span>Kullanılabilir bakiye</span>
                    <strong>{formatBalance(wallet)}</strong>
                  </div>
                  <button type="button" onClick={() => { setDepositWallet(wallet); setError(''); setNotice('') }}>
                    Para yatır <span aria-hidden="true">＋</span>
                  </button>
                </article>
              ))}

              {unopenedCurrencies.length > 0 && (
                <article className="open-wallet-card">
                  <div className="open-wallet-icon" aria-hidden="true">＋</div>
                  <div>
                    <span className="card-label">Yeni cüzdan</span>
                    <h3>Yeni bir para birimi ekleyin</h3>
                    <p>Her para birimi için yalnızca bir cüzdan açabilirsiniz.</p>
                  </div>
                  <div className="open-wallet-action">
                    <label htmlFor="wallet-currency">Para birimi</label>
                    <select
                      id="wallet-currency"
                      value={currencyToOpen}
                      onChange={(event) => setSelectedCurrency(event.target.value)}
                    >
                      {unopenedCurrencies.map((currency) => (
                        <option value={currency} key={currency}>{currency}</option>
                      ))}
                    </select>
                    <button type="button" onClick={handleOpenWallet} disabled={isOpening}>
                      {isOpening ? 'Açılıyor…' : 'Cüzdan aç'}
                    </button>
                  </div>
                </article>
              )}
            </div>
          )}
        </section>

        <section className="account-strip">
          <div className="avatar" aria-hidden="true">{initials(customer.fullName)}</div>
          <div>
            <span className="card-label">ArcPay müşterisi</span>
            <strong>{customer.fullName}</strong>
            <p>{customer.email}</p>
          </div>
          <div className="account-number">
            <span>Müşteri numarası</span>
            <strong>{customer.customerNumber}</strong>
          </div>
        </section>
      </main>

      {depositWallet && (
        <div className="modal-backdrop" role="presentation">
          <section className="deposit-modal" role="dialog" aria-modal="true" aria-labelledby="deposit-title">
            <button
              className="modal-close"
              type="button"
              aria-label="Para yatırma penceresini kapat"
              onClick={() => { setDepositWallet(null); setDepositAmount('') }}
            >×</button>
            <span className="currency-symbol">{currencyMeta[depositWallet.currency]?.symbol}</span>
            <p className="eyebrow">{depositWallet.currency} cüzdanı</p>
            <h2 id="deposit-title">Para yatırın</h2>
            <p>Bakiyeniz transaction kaydıyla birlikte atomik olarak güncellenecek.</p>
            <form onSubmit={handleDeposit}>
              <label htmlFor="deposit-amount">Tutar</label>
              <div className="amount-input">
                <input
                  id="deposit-amount"
                  type="text"
                  inputMode="decimal"
                  autoComplete="off"
                  placeholder="0,00"
                  value={depositAmount}
                  onChange={(event) => setDepositAmount(event.target.value)}
                  required
                />
                <span>{depositWallet.currency}</span>
              </div>
              <button className="primary-button" type="submit" disabled={isDepositing}>
                {isDepositing ? 'İşleniyor…' : 'Bakiyeye ekle'}
              </button>
            </form>
          </section>
        </div>
      )}
    </div>
  )
}
