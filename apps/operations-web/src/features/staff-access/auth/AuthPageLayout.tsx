import type { ReactNode } from 'react'
import './AuthPageLayout.css'

export function AuthPageLayout({ children, page = 'staff access' }: { children: ReactNode; page?: string }) {
  return <main className="auth-page" aria-label="SmartRail staff access">
    <section className="auth-page__brand" aria-label="SmartRail Operations">
      <div className="auth-page__brand-inner">
        <div className="auth-brand"><span className="auth-brand__mark" aria-hidden="true">SR</span><span><b>SmartRail</b><small>Operations</small></span></div>
        <div className="auth-page__intro"><span className="auth-page__eyebrow">RAILWAY OPERATIONS PLATFORM</span><h1>Clear operations.<br />Connected teams.</h1><p>Secure workspace access for railway staff coordinating station information, journeys, and incident response.</p></div>
        <div className="auth-page__features" aria-label="Workspace capabilities">
          <div><span aria-hidden="true">01</span><p><b>Operational visibility</b><small>Review published station and journey information.</small></p></div>
          <div><span aria-hidden="true">02</span><p><b>Role-aware workspaces</b><small>Controller and Station Staff views follow your assigned role.</small></p></div>
          <div><span aria-hidden="true">03</span><p><b>Protected actions</b><small>Operational changes are checked by SmartRail services.</small></p></div>
        </div>
        <div className="auth-page__rail" aria-hidden="true"><span /><span /><span /><span /><span /><span /><span /><span /><span /><span /><span /><span /></div>
        <small className="auth-page__brand-footer">SMART RAILWAY OPERATIONS</small>
      </div>
    </section>
    <section className="auth-page__content"><div className="auth-page__mobile-brand"><span className="auth-brand__mark" aria-hidden="true">SR</span><span><b>SmartRail</b><small>Operations</small></span></div>
      <div className="auth-card"><span className="auth-card__eyebrow">{page}</span>{children}</div>
      <p className="auth-page__footer">SmartRail Operations <span>·</span> Secure staff access</p>
    </section>
  </main>
}
