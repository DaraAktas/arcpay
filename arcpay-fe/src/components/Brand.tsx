import { Link } from 'react-router-dom'

export function Brand() {
  return (
    <Link className="brand" to="/" aria-label="ArcPay ana sayfa">
      <span className="brand-mark" aria-hidden="true">
        <svg viewBox="0 0 40 40" role="img">
          <path d="M7 27.5 19.7 6 33 27.5h-7.1l-6.2-11-6 11H7Z" />
          <path d="M13.7 27.5h12.2L30 34H10l3.7-6.5Z" className="brand-mark-accent" />
        </svg>
      </span>
      <span>ArcPay</span>
    </Link>
  )
}
