import { useAuth } from '../auth/auth-context'
import { Brand } from '../components/Brand'

function initials(fullName: string): string {
  return fullName
    .split(/\s+/)
    .slice(0, 2)
    .map((part) => part[0])
    .join('')
    .toLocaleUpperCase('tr-TR')
}

export function DashboardPage() {
  const { session, logout } = useAuth()
  if (!session) return null

  const { customer } = session

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
            <p className="eyebrow">Hesabım</p>
            <h1>Merhaba, {customer.fullName.split(' ')[0]}.</h1>
            <p>ArcPay hesabınız kullanıma hazır. Finansal yolculuğunuz burada başlıyor.</p>
          </div>
          <div className="phase-badge"><span>✓</span> Faz 2 tamamlandı</div>
        </section>

        <section className="dashboard-grid">
          <article className="profile-card">
            <div className="profile-card-top">
              <div className="avatar" aria-hidden="true">{initials(customer.fullName)}</div>
              <div>
                <span className="card-label">ArcPay müşterisi</span>
                <h2>{customer.fullName}</h2>
                <p>{customer.email}</p>
              </div>
            </div>
            <div className="customer-number">
              <span>Müşteri numaranız</span>
              <strong>{customer.customerNumber}</strong>
            </div>
          </article>

          <article className="next-card">
            <div className="next-card-icon" aria-hidden="true">₺</div>
            <span className="card-label">Sıradaki adım</span>
            <h2>İlk cüzdanınızı açın</h2>
            <p>TRY, USD veya EUR cüzdanlarınızı bir sonraki geliştirme fazında burada yöneteceksiniz.</p>
            <button type="button" disabled>Faz 3’te geliyor</button>
          </article>
        </section>

        <section className="security-strip">
          <div className="security-icon" aria-hidden="true">◆</div>
          <div>
            <strong>Hesabınız korunuyor</strong>
            <p>Oturumunuz süre kontrollü bir erişim anahtarıyla doğrulanıyor.</p>
          </div>
          <span>AKTİF</span>
        </section>
      </main>
    </div>
  )
}
