import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { useAuth } from '../auth/auth-context'
import { Brand } from '../components/Brand'
import { ApiError, transactionApi, walletApi } from '../lib/api'
import { getErrorMessage } from '../lib/form-errors'
import type { TransactionHistory, Wallet } from '../types/api'

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

function formatMoney(amount: number, currency: string): string {
  if (currency === 'XAU') {
    return `${new Intl.NumberFormat('tr-TR', { maximumFractionDigits: 8 }).format(amount)} XAU`
  }

  return new Intl.NumberFormat('tr-TR', {
    style: 'currency',
    currency,
    minimumFractionDigits: 2,
    maximumFractionDigits: 8,
  }).format(amount)
}

function parseAmount(value: string): number {
  return Number(value.replace(',', '.'))
}

export function DashboardPage() {
  const { session, logout } = useAuth()
  const [wallets, setWallets] = useState<Wallet[]>([])
  const [transactions, setTransactions] = useState<TransactionHistory[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isOpening, setIsOpening] = useState(false)
  const [selectedCurrency, setSelectedCurrency] = useState('TRY')
  const [depositWallet, setDepositWallet] = useState<Wallet | null>(null)
  const [depositAmount, setDepositAmount] = useState('')
  const [isDepositing, setIsDepositing] = useState(false)
  const [transferWallet, setTransferWallet] = useState<Wallet | null>(null)
  const [receiverCustomerNumber, setReceiverCustomerNumber] = useState('')
  const [transferAmount, setTransferAmount] = useState('')
  const [transferDescription, setTransferDescription] = useState('')
  const [isTransferring, setIsTransferring] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')

  useEffect(() => {
    if (!session) return
    let active = true

    const loadDashboard = async () => {
      try {
        const [walletResult, transactionResult] = await Promise.all([
          walletApi.list(session.accessToken),
          transactionApi.list(session.accessToken),
        ])
        if (active) {
          setWallets(walletResult)
          setTransactions(transactionResult)
        }
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

    void loadDashboard()
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

  const refreshTransactions = async () => {
    setTransactions(await transactionApi.list(session.accessToken))
  }

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

    const amount = parseAmount(depositAmount)
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
      await refreshTransactions()
      setNotice(`${result.wallet.currency} cüzdanınıza ${formatMoney(amount, result.wallet.currency)} yatırıldı.`)
      setDepositWallet(null)
      setDepositAmount('')
    } catch (caught) {
      setError(getErrorMessage(caught))
    } finally {
      setIsDepositing(false)
    }
  }

  const handleTransfer = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!transferWallet) return

    const amount = parseAmount(transferAmount)
    if (!Number.isFinite(amount) || amount <= 0) {
      setError('Gönderilecek tutar sıfırdan büyük olmalıdır.')
      return
    }

    setError('')
    setNotice('')
    setIsTransferring(true)
    try {
      const result = await transactionApi.transfer(
        receiverCustomerNumber.trim().toUpperCase(),
        amount,
        transferWallet.currency,
        crypto.randomUUID(),
        transferDescription,
        session.accessToken,
      )
      setWallets((current) =>
        current.map((wallet) => (wallet.id === result.senderWallet.id ? result.senderWallet : wallet)),
      )
      await refreshTransactions()
      setNotice(`${result.receiverCustomerNumber} numaralı müşteriye ${formatMoney(result.amount, result.currency)} gönderildi.`)
      setTransferWallet(null)
      setReceiverCustomerNumber('')
      setTransferAmount('')
      setTransferDescription('')
    } catch (caught) {
      setError(getErrorMessage(caught))
    } finally {
      setIsTransferring(false)
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
            <p>Bakiyelerinizi yönetin, güvenli para gönderin ve tüm hareketlerinizi izleyin.</p>
          </div>
          <div className="phase-badge"><span>✓</span> Faz 4 · ACID transfer</div>
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
                    <strong>{formatMoney(wallet.balance, wallet.currency)}</strong>
                  </div>
                  <div className="wallet-actions">
                    <button type="button" onClick={() => { setDepositWallet(wallet); setError(''); setNotice('') }}>
                      Para yatır <span aria-hidden="true">＋</span>
                    </button>
                    <button type="button" onClick={() => { setTransferWallet(wallet); setError(''); setNotice('') }}>
                      Para gönder <span aria-hidden="true">→</span>
                    </button>
                  </div>
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
                    <select id="wallet-currency" value={currencyToOpen} onChange={(event) => setSelectedCurrency(event.target.value)}>
                      {unopenedCurrencies.map((currency) => <option value={currency} key={currency}>{currency}</option>)}
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

        <section className="history-section" aria-labelledby="history-title">
          <div className="section-heading">
            <div>
              <span className="card-label">Hesap hareketleri</span>
              <h2 id="history-title">İşlem geçmişi</h2>
            </div>
            <span className="wallet-count">Son {transactions.length} işlem</span>
          </div>
          {transactions.length === 0 ? (
            <div className="history-empty">İlk para yatırma veya transferiniz burada görünecek.</div>
          ) : (
            <div className="history-list">
              {transactions.map((transaction) => {
                const incoming = transaction.direction === 'Incoming'
                const isDeposit = transaction.type === 'Deposit'
                return (
                  <article className="history-item" key={transaction.transactionRef}>
                    <span className={`history-icon ${incoming ? 'is-incoming' : 'is-outgoing'}`} aria-hidden="true">
                      {isDeposit ? '＋' : incoming ? '↙' : '↗'}
                    </span>
                    <div className="history-copy">
                      <strong>{isDeposit ? 'Para yatırma' : incoming ? 'Gelen transfer' : 'Giden transfer'}</strong>
                      <span>{transaction.counterpartyCustomerNumber ?? transaction.description ?? 'ArcPay cüzdanı'}</span>
                    </div>
                    <div className="history-meta">
                      <strong className={incoming ? 'is-positive' : ''}>
                        {incoming ? '+' : '-'}{formatMoney(transaction.amount, transaction.currency)}
                      </strong>
                      <span>{new Intl.DateTimeFormat('tr-TR', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(transaction.createdAt))}</span>
                    </div>
                  </article>
                )
              })}
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
          <section className="money-modal" role="dialog" aria-modal="true" aria-labelledby="deposit-title">
            <button className="modal-close" type="button" aria-label="Para yatırma penceresini kapat" onClick={() => { setDepositWallet(null); setDepositAmount('') }}>×</button>
            <span className="currency-symbol">{currencyMeta[depositWallet.currency]?.symbol}</span>
            <p className="eyebrow">{depositWallet.currency} cüzdanı</p>
            <h2 id="deposit-title">Para yatırın</h2>
            <p>Bakiyeniz işlem kaydıyla birlikte atomik olarak güncellenecek.</p>
            <form onSubmit={handleDeposit}>
              <label htmlFor="deposit-amount">Tutar</label>
              <div className="amount-input">
                <input id="deposit-amount" type="text" inputMode="decimal" autoComplete="off" placeholder="0,00" value={depositAmount} onChange={(event) => setDepositAmount(event.target.value)} required />
                <span>{depositWallet.currency}</span>
              </div>
              <button className="primary-button" type="submit" disabled={isDepositing}>
                {isDepositing ? 'İşleniyor…' : 'Bakiyeye ekle'}
              </button>
            </form>
          </section>
        </div>
      )}

      {transferWallet && (
        <div className="modal-backdrop" role="presentation">
          <section className="money-modal" role="dialog" aria-modal="true" aria-labelledby="transfer-title">
            <button className="modal-close" type="button" aria-label="Para gönderme penceresini kapat" onClick={() => { setTransferWallet(null); setReceiverCustomerNumber(''); setTransferAmount(''); setTransferDescription('') }}>×</button>
            <span className="currency-symbol">→</span>
            <p className="eyebrow">{transferWallet.currency} cüzdanı</p>
            <h2 id="transfer-title">Para gönderin</h2>
            <p>İki bakiye tek ACID işleminde güncellenir; tekrar eden istekler ikinci kez işlenmez.</p>
            <form onSubmit={handleTransfer}>
              <label htmlFor="receiver-number">Alıcı müşteri numarası</label>
              <input className="modal-input" id="receiver-number" value={receiverCustomerNumber} onChange={(event) => setReceiverCustomerNumber(event.target.value)} placeholder="ARC-1000000002" pattern="ARC-[0-9]{10}" required />
              <label htmlFor="transfer-amount">Tutar</label>
              <div className="amount-input">
                <input id="transfer-amount" type="text" inputMode="decimal" autoComplete="off" placeholder="0,00" value={transferAmount} onChange={(event) => setTransferAmount(event.target.value)} required />
                <span>{transferWallet.currency}</span>
              </div>
              <label htmlFor="transfer-description">Açıklama <span className="optional-label">isteğe bağlı</span></label>
              <input className="modal-input" id="transfer-description" value={transferDescription} onChange={(event) => setTransferDescription(event.target.value)} maxLength={500} placeholder="Örn. Yemek payı" />
              <button className="primary-button" type="submit" disabled={isTransferring}>
                {isTransferring ? 'Gönderiliyor…' : 'Transferi tamamla'}
              </button>
            </form>
          </section>
        </div>
      )}
    </div>
  )
}
