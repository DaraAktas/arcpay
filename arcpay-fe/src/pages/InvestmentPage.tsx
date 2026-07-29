import { useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../auth/auth-context'
import { Brand } from '../components/Brand'
import { ApiError, investmentApi } from '../lib/api'
import { getErrorMessage } from '../lib/form-errors'
import type { MarketQuote, Portfolio } from '../types/api'

function formatMoney(amount: number, currency: string): string {
  return new Intl.NumberFormat('tr-TR', { style: 'currency', currency, maximumFractionDigits: 8 }).format(amount)
}

export function InvestmentPage() {
  const { session, logout } = useAuth()
  const [market, setMarket] = useState<MarketQuote[]>([])
  const [portfolio, setPortfolio] = useState<Portfolio | null>(null)
  const [selectedQuote, setSelectedQuote] = useState<MarketQuote | null>(null)
  const [quantity, setQuantity] = useState('1')
  const [simulateFailure, setSimulateFailure] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [isPurchasing, setIsPurchasing] = useState(false)
  const [pageError, setPageError] = useState('')
  const [purchaseError, setPurchaseError] = useState('')
  const [notice, setNotice] = useState('')

  useEffect(() => {
    if (!session) return
    Promise.all([
      investmentApi.market(session.accessToken),
      investmentApi.portfolio(session.accessToken),
    ]).then(([quotes, portfolioResult]) => {
      setMarket(quotes)
      setPortfolio(portfolioResult)
    }).catch((caught) => {
      if (caught instanceof ApiError && caught.status === 401) logout()
      else setPageError(getErrorMessage(caught))
    }).finally(() => setIsLoading(false))
  }, [logout, session])

  if (!session) return null

  const handlePurchase = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!selectedQuote) return
    const parsedQuantity = Number(quantity.replace(',', '.'))
    if (!Number.isFinite(parsedQuantity) || parsedQuantity <= 0) {
      setPurchaseError('Adet sıfırdan büyük olmalıdır.')
      return
    }

    setPurchaseError('')
    setNotice('')
    setIsPurchasing(true)
    try {
      const purchase = await investmentApi.purchase(
        selectedQuote.symbol, parsedQuantity, crypto.randomUUID(), simulateFailure, session.accessToken)
      setPortfolio(await investmentApi.portfolio(session.accessToken))
      setNotice(`${purchase.quantity} ${purchase.symbol}, ${formatMoney(purchase.totalAmount, purchase.currency)} karşılığında portföyünüze eklendi.`)
      setSelectedQuote(null)
      setQuantity('1')
      setSimulateFailure(false)
    } catch (caught) {
      setPurchaseError(getErrorMessage(caught))
    } finally {
      setIsPurchasing(false)
    }
  }

  return (
    <div className="dashboard-shell investment-shell">
      <header className="dashboard-header">
        <Brand />
        <nav className="main-nav" aria-label="Ana menü">
          <Link to="/hesabim">Cüzdanlar</Link>
          <Link className="is-active" to="/yatirimlar">Yatırımlar</Link>
        </nav>
        <div className="header-actions">
          <span className="session-indicator"><i /> Güvenli oturum</span>
          <button className="text-button" type="button" onClick={logout}>Çıkış yap</button>
        </div>
      </header>

      <main className="dashboard-main">
        <section className="welcome-row">
          <div>
            <p className="eyebrow">Piyasa ve portföy</p>
            <h1>Yatırım rotanızı oluşturun.</h1>
            <p>Fiyatları 60 saniyelik cache üzerinden izleyin; cüzdan bakiyenizle güvenli alım yapın.</p>
          </div>
          <div className="phase-badge"><span>✓</span> Faz 5 · Saga + telafi</div>
        </section>

        {(pageError || notice) && (
          <div className={`dashboard-notice ${pageError ? 'is-error' : ''}`} role={pageError ? 'alert' : 'status'}>
            {pageError || notice}
            <button type="button" aria-label="Bildirimi kapat" onClick={() => { setPageError(''); setNotice('') }}>×</button>
          </div>
        )}

        <section className="market-section" aria-labelledby="market-title">
          <div className="section-heading">
            <div><span className="card-label">ArcPay Demo Market</span><h2 id="market-title">Piyasa</h2></div>
            <span className="wallet-count">60 sn cache</span>
          </div>
          {isLoading ? <div className="wallet-loading">Piyasa verileri yükleniyor…</div> : (
            <div className="market-grid">
              {market.map((quote) => (
                <article className="market-card" key={quote.symbol}>
                  <div className="market-symbol"><strong>{quote.symbol}</strong><span>{quote.name}</span></div>
                  <strong className="market-price">{formatMoney(quote.price, quote.currency)}</strong>
                  <span className={quote.changePercent >= 0 ? 'market-change is-positive' : 'market-change'}>
                    {quote.changePercent >= 0 ? '+' : ''}{quote.changePercent}%
                  </span>
                  <button type="button" onClick={() => { setSelectedQuote(quote); setPurchaseError(''); setPageError(''); setNotice('') }}>Satın al</button>
                </article>
              ))}
            </div>
          )}
        </section>

        <section className="portfolio-section" aria-labelledby="portfolio-title">
          <div className="section-heading">
            <div><span className="card-label">Varlıklarınız</span><h2 id="portfolio-title">Portföy</h2></div>
            <span className="wallet-count">{portfolio?.holdings.length ?? 0} varlık</span>
          </div>
          {!portfolio?.holdings.length ? (
            <div className="history-empty">İlk yatırımınız burada görünecek.</div>
          ) : (
            <div className="portfolio-list">
              {portfolio.holdings.map((holding) => (
                <article key={holding.symbol}>
                  <div><strong>{holding.symbol}</strong><span>Ortalama maliyet {formatMoney(holding.averageCost, holding.currency)}</span></div>
                  <strong>{holding.quantity} adet</strong>
                </article>
              ))}
            </div>
          )}
        </section>
      </main>

      {selectedQuote && (
        <div className="modal-backdrop" role="presentation">
          <section className="money-modal" role="dialog" aria-modal="true" aria-labelledby="purchase-title">
            <button className="modal-close" type="button" aria-label="Yatırım penceresini kapat" onClick={() => { setSelectedQuote(null); setPurchaseError('') }}>×</button>
            <span className="currency-symbol">↗</span>
            <p className="eyebrow">{selectedQuote.symbol} · {selectedQuote.name}</p>
            <h2 id="purchase-title">Varlık satın alın</h2>
            <p>Tahsilat WalletApi’de yapılır; portföy kaydı başarısız olursa saga tutarı otomatik iade eder.</p>
            {purchaseError && <div className="modal-notice" role="alert">{purchaseError}</div>}
            <form onSubmit={handlePurchase}>
              <label htmlFor="purchase-quantity">Adet</label>
              <input className="modal-input" id="purchase-quantity" inputMode="decimal" value={quantity} onChange={(event) => setQuantity(event.target.value)} required />
              <div className="purchase-summary"><span>Tahmini toplam</span><strong>{formatMoney(selectedQuote.price * (Number(quantity.replace(',', '.')) || 0), selectedQuote.currency)}</strong></div>
              <label className="saga-demo-toggle">
                <input type="checkbox" checked={simulateFailure} onChange={(event) => setSimulateFailure(event.target.checked)} />
                Telafi senaryosunu test et <span>(tahsilat otomatik iade edilir)</span>
              </label>
              <button className="primary-button" type="submit" disabled={isPurchasing}>{isPurchasing ? 'İşleniyor…' : 'Satın al'}</button>
            </form>
          </section>
        </div>
      )}
    </div>
  )
}
