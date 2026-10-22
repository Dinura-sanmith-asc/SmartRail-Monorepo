import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { useAuth } from 'react-oidc-context'
import { useRuntimeConfig } from '../../config/runtimeConfigContext'
import { ApiRequestError } from '../../shared/api/fetchJson'
import { useLocalSession } from '../staff-access/auth/localSessionContext'
import { WorkspaceNavigation } from '../staff-access/workspaces/WorkspaceNavigation'
import { apiErrorMessage, operationsApi, type DriverAssignment } from '../operations/operationsApi'
import '../operations/operations.css'
import './driver-workspace.css'
import { LanguageSelect, useLanguage } from '../../shared/i18n/language'

function formatTime(value?: string | null) {
  if (!value) return 'Not available'
  const language = document.documentElement.lang || 'en'
  return new Intl.DateTimeFormat(`${language}-LK`, { timeZone: 'Asia/Colombo', dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))
}

export function DriverWorkspace() {
  const { t } = useLanguage()
  const auth = useAuth()
  const { apiBaseUrl } = useRuntimeConfig()
  const session = useLocalSession()
  const options = useMemo(() => ({ apiBaseUrl, accessToken: auth.user?.access_token ?? null }), [apiBaseUrl, auth.user?.access_token])
  const [assignments, setAssignments] = useState<DriverAssignment[]>([])
  const [loading, setLoading] = useState(true)
  const [busyId, setBusyId] = useState('')
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')

  const load = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const result = await operationsApi.driverAssignments(options)
      setAssignments(result.items)
    } catch (failure) {
      setError(apiErrorMessage(failure))
      if (failure instanceof ApiRequestError && failure.status === 401) session.invalidateLocalSession()
    } finally {
      setLoading(false)
    }
  }, [options, session])

  useEffect(() => { void Promise.resolve().then(load) }, [load])

  async function submit(event: FormEvent<HTMLFormElement>, assignment: DriverAssignment) {
    event.preventDefault()
    const form = event.currentTarget
    const text = String(new FormData(form).get('observation') ?? '').trim()
    if (!text) return
    setBusyId(assignment.assignmentId)
    setError('')
    setNotice('')
    try {
      await operationsApi.submitDriverObservation(assignment.journey.snapshot.journeyInstanceId, text, options)
      form.reset()
      setNotice('Your progress observation was submitted to Control for review.')
    } catch (failure) {
      setError(apiErrorMessage(failure))
      if (failure instanceof ApiRequestError && failure.status === 401) session.invalidateLocalSession()
    } finally {
      setBusyId('')
    }
  }

  return <div className="operations-layout driver-layout">
    <aside className="operations-sidebar">
      <div className="operations-brand"><span className="operations-brand__mark" aria-hidden="true">S</span><div><b>SmartRail</b><small>Driver operations</small></div></div>
      <p className="operations-sidebar__role">DRIVER WORKSPACE</p>
      <div className="operations-sidebar__footer"><WorkspaceNavigation /></div>
    </aside>
    <main className="operations driver-workspace">
      <header className="operations__header">
        <div><p className="operations__eyebrow">{t('Driver Workspace')} <span>·</span> Sri Lanka</p><h1>{t('Your assigned journeys')}</h1><p className="operations__subtitle">{t('Review published journey information and send progress observations to Control.')}</p></div>
        <div className="driver-header-actions"><LanguageSelect /><button className="button button--secondary" type="button" onClick={() => void load()} disabled={loading}>{loading ? 'Refreshing…' : t('Refresh assignments')}</button></div>
      </header>
      <p className="driver-boundary"><strong>{t('Operational boundary')}</strong> {t('Your report is evidence for Control. It does not publish or change official journey status.')}</p>
      {error && <div className="operations__feedback operations__feedback--error" role="alert"><span>{error}</span><button className="button button--secondary button--small" type="button" onClick={() => void load()}>Try again</button></div>}
      {notice && <p className="driver-notice" role="status">{notice}</p>}
      {loading ? <section className="operations__panel" aria-label="Loading assigned journeys"><div className="operations-skeleton-list"><span /><span /><span /></div></section>
          : assignments.length === 0 ? <section className="operations__panel driver-empty" role="status"><span aria-hidden="true">✓</span><h2>{t('No current assignments')}</h2><p>{t('There are no journeys assigned to your driver account right now.')}</p></section>
          : <div className="driver-assignment-list">{assignments.map((assignment) => {
            const journey = assignment.journey.snapshot
            const first = journey.stops[0]
            const last = journey.stops[journey.stops.length - 1]
            return <article className="operations__panel driver-card" key={assignment.assignmentId}>
              <div className="driver-card__heading"><div><p className="operations__eyebrow">SERVICE {journey.serviceId.slice(0, 8)}</p><h2>{first?.stationName ?? 'Origin unavailable'} <span aria-hidden="true">→</span> {last?.stationName ?? 'Destination unavailable'}</h2></div><span className={`driver-status driver-status--${journey.status.toLowerCase()}`}>{t(journey.status)}</span></div>
              <div className="driver-facts"><div><small>{t('Operation date')}</small><b>{journey.operationDate}</b></div><div><small>{t('Current status')}</small><b>{t(journey.status)}</b></div><div><small>{t('Last official update')}</small><b>{formatTime(journey.lastUpdatedAt)}</b></div><div><small>{t('Estimate')}</small><b>{journey.eta ? formatTime(journey.eta.value) : t('Not available')}</b></div></div>
              {assignment.journey.updates.length > 0 && <section className="driver-updates" aria-label={t('Published journey updates')}><h3>{t('Recent official updates')}</h3><ol>{assignment.journey.updates.slice(0, 3).map((update) => <li key={update.updateId}><span>{t(update.status)}</span><small>Revision {update.revision} · {formatTime(update.publishedAt)}</small></li>)}</ol></section>}
              <h3>{t('Stops')}</h3><ol className="driver-stops">{journey.stops.map((stop) => <li key={stop.journeyStopId}><span>{stop.stationName}</span><small>{stop.platform ? `Platform ${stop.platform}` : t('Platform unavailable')} · {formatTime(stop.operationalDepartureAt ?? stop.plannedDepartureAt)}</small></li>)}</ol>
              <form className="driver-report" onSubmit={(event) => void submit(event, assignment)}><label className="operations-control" htmlFor={`observation-${assignment.assignmentId}`}><span>{t('Report journey progress')}</span><textarea id={`observation-${assignment.assignmentId}`} name="observation" required maxLength={4000} rows={3} placeholder="Describe what you observed. Avoid safety critical control instructions." /></label><div><small>Sent to Control with the journey revision you reviewed.</small><button className="button button--primary" type="submit" disabled={busyId === assignment.assignmentId}>{busyId === assignment.assignmentId ? t('Submitting…') : t('Submit observation')}</button></div></form>
            </article>
          })}</div>}
    </main>
  </div>
}
