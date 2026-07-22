import type { PropsWithChildren } from 'react'
import { Brand } from './Brand'

interface AuthLayoutProps extends PropsWithChildren {
  eyebrow: string
  title: string
  description: string
}

export function AuthLayout({ eyebrow, title, description, children }: AuthLayoutProps) {
  return (
    <main className="auth-shell">
      <section className="auth-story" aria-label="ArcPay tanıtım">
        <Brand />
        <div className="story-copy">
          <p className="eyebrow">Yeni nesil finans deneyimi</p>
          <h1>
            Paranızın yönü,
            <br />
            <span>sizin elinizde.</span>
          </h1>
          <p>
            Tek bir güvenli hesapla farklı para birimlerini yönetin, aktarın ve geleceğe
            yatırım yapın.
          </p>
        </div>
        <div className="story-metric" aria-label="ArcPay güvenlik bilgisi">
          <div className="metric-icon" aria-hidden="true">✓</div>
          <div>
            <strong>Güvenli oturum</strong>
            <span>JWT ile uçtan uca doğrulama</span>
          </div>
        </div>
        <div className="orb orb-one" />
        <div className="orb orb-two" />
      </section>

      <section className="auth-panel">
        <div className="mobile-brand"><Brand /></div>
        <div className="form-wrap">
          <p className="eyebrow">{eyebrow}</p>
          <h2>{title}</h2>
          <p className="form-intro">{description}</p>
          {children}
        </div>
        <p className="legal-note">© 2026 ArcPay · Güvenle tasarlandı.</p>
      </section>
    </main>
  )
}
