import { Fragment, useCallback, useEffect, useMemo, useState, type FormEvent, type ReactNode } from 'react'
import { useAuth } from 'react-oidc-context'
import { useRuntimeConfig } from '../../config/runtimeConfigContext'
import { ApiRequestError } from '../../shared/api/fetchJson'
import { useLocalSession } from '../staff-access/auth/localSessionContext'
import { useIdentity } from '../staff-access/identity/identityContext'
import { WorkspaceNavigation } from '../staff-access/workspaces/WorkspaceNavigation'
import { apiErrorMessage, operationsApi, type Departure, type DriverObservation, type Incident, type Journey, type JourneyUpdate, type Station } from './operationsApi'
import { ControllerOverview } from './ControllerOverview'
import { TrainServiceManagement } from './TrainServiceManagement'
import { RouteManagement } from './RouteManagement'
import './operations.css'
import { LanguageSelect, useLanguage } from '../../shared/i18n/language'

type Tab = 'overview' | 'journeys' | 'incidents' | 'stations' | 'routes' | 'services'
type Column<T> = { key: string; label: string; render: (row: T) => ReactNode }

function colomboDate() {
  const parts = new Intl.DateTimeFormat('en', { timeZone: 'Asia/Colombo', year: 'numeric', month: '2-digit', day: '2-digit' }).formatToParts(new Date())
  const values = Object.fromEntries(parts.map(({ type, value }) => [type, value]))
  return `${values.year}-${values.month}-${values.day}`
}

function timestamp(value?: string | null) {
  const language = document.documentElement.lang || 'en'
  return value ? new Intl.DateTimeFormat(`${language}-LK`, { timeZone: 'Asia/Colombo', dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)) : 'Unknown'
}

const roleMenuItems: Record<'Controller' | 'Station Staff', Record<Tab, { label: string; description: string } | null>> = {
  Controller: {
    overview: { label: 'Operations overview', description: 'Stations and departure status' },
    journeys: { label: 'Journey control', description: 'Journey status and history' },
    incidents: { label: 'Incident management', description: 'Authorized incident actions' },
    stations: { label: 'Stations', description: 'Manage railway stations' },
    routes: { label: 'Routes', description: 'Railway routes and ordered stops' },
    services: { label: 'Train Services', description: 'Manage train services' },
  },
  'Station Staff': {
    overview: { label: 'Station overview', description: 'Local departures and station details' },
    journeys: null,
    incidents: { label: 'Incident reporting', description: 'Scoped station incidents' },
    stations: null,
    routes: null,
    services: null,
  },
}

type SidebarGroupId = 'main' | 'operations' | 'railway' | 'system'
type SidebarItem = { id: string; label: string; description: string; icon: string; target?: Tab }

const sidebarGroups: { id: SidebarGroupId; label: string; items: SidebarItem[] }[] = [
  { id: 'main', label: 'Main', items: [
    { id: 'dashboard', label: 'Dashboard', description: 'Overall railway operations', icon: 'dashboard', target: 'overview' },
    { id: 'live-journeys', label: 'Live Journeys', description: "Today's trains and official status", icon: 'train', target: 'journeys' },
    { id: 'station-departures', label: 'Station Departures', description: 'Departure boards and platforms', icon: 'clock' },
  ] },
  { id: 'operations', label: 'Operations', items: [
    { id: 'journey-status', label: 'Journey Status Control', description: 'Delays, cancellations and platform changes', icon: 'signal', target: 'journeys' },
    { id: 'incident-management', label: 'Incident Management', description: 'Report, track and close incidents', icon: 'alert', target: 'incidents' },
    { id: 'driver-operations', label: 'Driver Operations', description: 'Assignments and observations', icon: 'people' },
  ] },
  { id: 'railway', label: 'Railway Network', items: [
    { id: 'stations', label: 'Stations', description: 'Add, edit and manage stations', icon: 'pin', target: 'stations' },
    { id: 'routes', label: 'Routes', description: 'Railway routes and ordered stops', icon: 'route', target: 'routes' },
    { id: 'train-services', label: 'Train Services', description: 'Service numbers and names', icon: 'train', target: 'services' },
    { id: 'timetables', label: 'Timetables & Schedules', description: 'Calling points, revisions and operating dates', icon: 'calendar' },
  ] },
  { id: 'system', label: 'System', items: [
    { id: 'my-profile', label: 'My Profile', description: '', icon: 'profile' },
  ] },
]

export function OperationsWorkspace({ role }: { role: 'Controller' | 'Station Staff' }) {
  const { t } = useLanguage()
  const auth = useAuth()
  const { apiBaseUrl } = useRuntimeConfig()
  const session = useLocalSession()
  const { identity } = useIdentity()
  const token = auth.user?.access_token ?? null
  const options = useMemo(() => ({ apiBaseUrl, accessToken: token }), [apiBaseUrl, token])
  const [tab, setTab] = useState<Tab>('overview')
  const [openGroup, setOpenGroup] = useState<SidebarGroupId | null>('main')
  const [activeMenuItem, setActiveMenuItem] = useState('dashboard')
  const [stations, setStations] = useState<Station[]>([])
  const [stationId, setStationId] = useState('')
  const [date, setDate] = useState(colomboDate)
  const [departures, setDepartures] = useState<Departure[]>([])
  const [incidents, setIncidents] = useState<Incident[]>([])
  const [journey, setJourney] = useState<Journey | null>(null)
  const [updates, setUpdates] = useState<JourneyUpdate[]>([])
  const [observations, setObservations] = useState<DriverObservation[]>([])
  const [departureQuery, setDepartureQuery] = useState('')
  const [departureStatus, setDepartureStatus] = useState('all')
  const [incidentQuery, setIncidentQuery] = useState('')
  const [incidentState, setIncidentState] = useState('all')
  const [stationsLoading, setStationsLoading] = useState(true)
  const [incidentsLoading, setIncidentsLoading] = useState(true)
  const [departuresLoading, setDeparturesLoading] = useState(false)
  const [mutating, setMutating] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')

  const onError = useCallback((failure: unknown) => {
    setError(apiErrorMessage(failure))
    if (failure instanceof ApiRequestError && failure.status === 401) session.invalidateLocalSession()
  }, [session])
  const onOverviewUnauthorized = useCallback(() => session.invalidateLocalSession(), [session])

  function selectTab(nextTab: Tab, group: SidebarGroupId, itemId?: string) {
    setTab(nextTab)
    setOpenGroup(group)
    setActiveMenuItem(itemId ?? (nextTab === 'incidents' ? 'incident-management'
      : nextTab === 'journeys' ? 'journey-status'
        : nextTab === 'stations' ? 'stations'
          : nextTab === 'routes' ? 'routes'
            : nextTab === 'services' ? 'train-services' : 'dashboard'))
  }

  const loadStations = useCallback(async () => {
    setStationsLoading(true)
    try {
      const data = await operationsApi.stations(options)
      setStations(data.items)
      setStationId((current) => current || data.items[0]?.stationId || '')
    } catch (failure) {
      onError(failure)
    } finally {
      setStationsLoading(false)
    }
  }, [onError, options])

  const loadIncidents = useCallback(async () => {
    if (!token) return
    setIncidentsLoading(true)
    try {
      const data = await operationsApi.incidents(options)
      setIncidents(data.items)
    } catch (failure) {
      onError(failure)
    } finally {
      setIncidentsLoading(false)
    }
  }, [onError, options, token])

  const loadDepartures = useCallback(async () => {
    if (!stationId || !date) return
    setDeparturesLoading(true)
    setError('')
    try {
      setDepartures(await operationsApi.departures(stationId, date, options))
    } catch (failure) {
      onError(failure)
    } finally {
      setDeparturesLoading(false)
    }
  }, [date, onError, options, stationId])

  const refresh = useCallback(async () => {
    setError('')
    setNotice('')
    await Promise.all([loadStations(), loadIncidents()])
    if (stationId) await loadDepartures()
  }, [loadDepartures, loadIncidents, loadStations, stationId])

  useEffect(() => { void Promise.resolve().then(loadStations) }, [loadStations])
  useEffect(() => { void Promise.resolve().then(loadIncidents) }, [loadIncidents])
  useEffect(() => { if (stationId) void Promise.resolve().then(loadDepartures) }, [loadDepartures, stationId])
  useEffect(() => {
    if (!notice) return
    const timeout = window.setTimeout(() => setNotice(''), 5000)
    return () => window.clearTimeout(timeout)
  }, [notice])

  async function openJourney(id: string) {
    setError('')
    setNotice('')
    setDeparturesLoading(true)
    try {
      const [snapshot, history] = await Promise.all([
        operationsApi.journey(id, options),
        operationsApi.journeyUpdates(id, options),
      ])
      setJourney(snapshot)
      setUpdates(history)
      selectTab('journeys', 'operations')
      if (role === 'Controller') {
        const evidence = await operationsApi.driverObservations(id, options)
        setObservations(evidence.items)
      }
    } catch (failure) {
      onError(failure)
    } finally {
      setDeparturesLoading(false)
    }
  }

  async function submitIncident(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    const categoryCode = String(form.get('categoryCode') ?? '').trim()
    const description = String(form.get('description') ?? '').trim()
    const resourceId = String(form.get('resourceId') ?? '')
    if (!categoryCode || !description || !resourceId) return
    const formElement = event.currentTarget
    setMutating(true)
    setError('')
    try {
      await operationsApi.createIncident({ categoryCode, description, associations: [{ kind: 'Station', resourceId }] }, options)
      formElement.reset()
      setNotice('Incident report submitted.')
      await loadIncidents()
    } catch (failure) {
      onError(failure)
    } finally {
      setMutating(false)
    }
  }

  async function transitionIncident(incident: Incident) {
    const nextState = incident.state === 'Reported' ? 'In Progress'
      : incident.state === 'In Progress' ? 'Resolved'
        : incident.state === 'Resolved' ? 'Closed' : null
    if (!nextState) return
    if (nextState === 'Closed' && !window.confirm('Close this incident? Closing is terminal and cannot be reversed.')) return
    setMutating(true)
    setError('')
    try {
      await operationsApi.updateIncident(incident.incidentId, { expectedRevision: incident.revision, state: nextState }, options)
      setNotice(`Incident moved to ${nextState}.`)
      await loadIncidents()
    } catch (failure) {
      onError(failure)
    } finally {
      setMutating(false)
    }
  }

  async function publishStatus(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!journey) return
    const status = String(new FormData(event.currentTarget).get('status'))
    if (status === journey.status) return
    if (status === 'Cancelled' && !window.confirm('Confirm cancellation? This publishes an official operational status.')) return
    setMutating(true)
    setError('')
    try {
      const updated = await operationsApi.publishStatus(journey.journeyInstanceId, {
        expectedJourneyRevision: journey.revision,
        status,
      }, options)
      setJourney(updated)
      setUpdates(await operationsApi.journeyUpdates(updated.journeyInstanceId, options))
      setNotice('Official journey status published.')
    } catch (failure) {
      onError(failure)
    } finally {
      setMutating(false)
    }
  }

  async function saveStop(event: FormEvent<HTMLFormElement>, stopId: string) {
    event.preventDefault()
    if (!journey) return
    const platform = String(new FormData(event.currentTarget).get('platform') ?? '').trim()
    if (!platform) return
    setMutating(true)
    setError('')
    try {
      const updated = await operationsApi.updateStop(journey.journeyInstanceId, stopId, {
        expectedJourneyRevision: journey.revision,
        platform,
      }, options)
      setJourney(updated)
      setUpdates(await operationsApi.journeyUpdates(updated.journeyInstanceId, options))
      setNotice('Stop platform update published.')
    } catch (failure) {
      onError(failure)
    } finally {
      setMutating(false)
    }
  }

  const selectedStation = stations.find((item) => item.stationId === stationId)
  const visibleDepartures = departures.filter((item) => {
    const query = departureQuery.trim().toLowerCase()
    const matchesQuery = !query || `${item.stationName} ${item.serviceId} ${item.journeyInstanceId} ${item.status}`.toLowerCase().includes(query)
    return matchesQuery && (departureStatus === 'all' || item.status === departureStatus)
  })
  const visibleIncidents = incidents.filter((item) => {
    const query = incidentQuery.trim().toLowerCase()
    const matchesQuery = !query || `${item.categoryCode} ${item.description} ${item.incidentId}`.toLowerCase().includes(query)
    return matchesQuery && (incidentState === 'all' || item.state === incidentState)
  })

  return (
    <div className="operations-layout">
          <a className="operations__skip-link" href="#operations-content">{t('Skip to operations content')}</a>
      <aside className="operations-sidebar" aria-label="Application sidebar">
        <div className="operations-brand"><span className="operations-brand__mark" aria-hidden="true">SR</span><span><b>SmartRail</b><small>{t('Operations control')}</small></span></div>
        <div className="operations-sidebar__role"><small>{t('ACTIVE WORKSPACE')}</small><strong>{t(role)}</strong></div>
        <nav className="operations-menu" aria-label={t('Workspace sections')}>
          {sidebarGroups.map((group) => {
            const expanded = openGroup === group.id
            return <section className="operations-menu__group" key={group.id}>
              <button className={`operations-menu__group-toggle${expanded ? ' is-expanded' : ''}`} type="button"
                aria-expanded={expanded} aria-controls={`operations-menu-${group.id}`} onClick={() => setOpenGroup(expanded ? null : group.id)}>
                <b>{t(group.label)}</b><span className="operations-menu__chevron" aria-hidden="true" />
              </button>
              <div className="operations-menu__items" id={`operations-menu-${group.id}`} hidden={!expanded}>
                {group.items.map((item) => {
                  const available = item.target ? roleMenuItems[role][item.target] !== null : false
                  const active = available && activeMenuItem === item.id
                  return <button key={item.id} type="button" aria-label={t(item.label)} aria-disabled={!available}
                    className={`operations-menu__item${active ? ' is-active' : ''}${available ? '' : ' is-placeholder'}`}
                    aria-current={active ? 'page' : undefined} onClick={() => { if (available && item.target) selectTab(item.target, group.id, item.id) }}>
                    <SidebarIcon name={item.icon} />
                    <span><b>{t(item.label)}</b>{item.description && <small>{t(item.description)}</small>}</span>
                  </button>
                })}
              </div>
            </section>
          })}
        </nav>
        <div className="operations-sidebar__footer"><WorkspaceNavigation /></div>
      </aside>

      <main id="operations-content" className="operations" aria-labelledby="operations-heading">
        <header className="operations__header">
          <div><p className="operations__eyebrow">{t('Network operations')} <span>/</span> {t(role)}</p>
            <h1 id="operations-heading">{t(role === 'Controller' ? 'Control room' : 'Station operations')}</h1>
            <p className="operations__subtitle">{t(role === 'Controller' ? 'Monitor railway movement and coordinate official operational updates.' : 'Review station information, departures, and incidents available to your role.')}</p>
          </div>
          <div className="operations__header-actions">
            <LanguageSelect /><span className="operations__identity">{identity?.roles.map(t).join(' · ')}</span>
            <button className="button button--secondary" type="button" onClick={() => void refresh()} disabled={departuresLoading || stationsLoading}>
              <RefreshIcon />{t(departuresLoading || stationsLoading ? 'Refreshing' : 'Refresh data')}
            </button>
          </div>
        </header>

        <div className="operations__contextbar"><span>{t('Operational data')} · SmartRail API</span><span>{t('Station-local date')} · {date || t('Choose date')} (Asia/Colombo)</span></div>

        {error && <div className="operations__feedback operations__feedback--error" role="alert"><span><b>{t('Request needs attention')}</b><small>{t(error)}</small></span><button className="button button--secondary" type="button" onClick={() => void refresh()}>{t('Retry request')}</button></div>}
        {notice && <div className="operations-toast" role="status" aria-live="polite"><span><b>✓</b> {t(notice)}</span><button type="button" aria-label={t('Dismiss notification')} onClick={() => setNotice('')}>×</button></div>}

        {tab === 'overview' && (
          <>
            {role === 'Controller' && <ControllerOverview
              options={options}
              onOpenJourney={(journeyId) => void openJourney(journeyId)}
              onOpenIncidents={() => selectTab('incidents', 'operations')}
              onUnauthorized={onOverviewUnauthorized}
            />}
            <section className="operations__stats" aria-label={t('Operational summary')}>
              <SummaryCard title={t('Published stations')} value={stations.length} loading={stationsLoading} hint={t('From the published railway network')} />
              <SummaryCard title={t('Station departures')} value={departures.length} loading={departuresLoading} hint={t('For the selected date and station')} />
              <SummaryCard title={t('Visible incidents')} value={incidents.length} loading={incidentsLoading} hint={t('Filtered by your current access')} />
            </section>
            {selectedStation && <section className="operations__station-card" aria-label="Selected station information">
              <div className="operations__station-icon" aria-hidden="true"><StationIcon /></div>
              <div><span className="operations__section-kicker">{t('SELECTED STATION')}</span><h2>{selectedStation.name}</h2><p>{selectedStation.locationDetails?.length ? selectedStation.locationDetails.join(' · ') : t('No additional location details are published.')}</p>
                {selectedStation.serviceDetails?.length ? <small>{selectedStation.serviceDetails.join(' · ')}</small> : null}</div>
              <code>{selectedStation.stationId}</code>
            </section>}
            <section className="operations__panel">
              <PanelHeading title={t('Departure board')} description={t('Published journey departures and current operational status.')} />
              <DepartureFilters stations={stations} stationId={stationId} setStationId={setStationId} date={date} setDate={setDate} loading={departuresLoading} refresh={() => void loadDepartures()} />
              <div className="operations__table-tools">
                <SearchInput label={t('Search departures')} value={departureQuery} onChange={setDepartureQuery} placeholder={t('Service, station, journey…')} />
                <label className="operations-control"><span>{t('Operational status')}</span><select value={departureStatus} onChange={(event) => setDepartureStatus(event.target.value)}><option value="all">{t('All statuses')}</option>{['Scheduled', 'Running', 'Delayed', 'Interrupted', 'Cancelled', 'Completed', 'Terminated'].map((status) => <option key={status} value={status}>{t(status)}</option>)}</select></label>
              </div>
              {departuresLoading ? <TableSkeleton /> : visibleDepartures.length ? <DepartureTable departures={visibleDepartures} onOpen={openJourney} /> : <EmptyState title={t(departures.length ? 'No matching departures' : 'No departures found')} text={t(departures.length ? 'Try changing the search or status filters.' : 'There are no departures for this station and date.')} />}
            </section>
          </>
        )}

        {tab === 'journeys' && role === 'Controller' && (
          <>
            <section className="operations__panel">
              <PanelHeading title={t('Journey control')} description={t('Select a dated journey from the supported departure feed to inspect its revision and published history.')} />
              <DepartureFilters stations={stations} stationId={stationId} setStationId={setStationId} date={date} setDate={setDate} loading={departuresLoading} refresh={() => void loadDepartures()} />
              {departuresLoading ? <TableSkeleton /> : <DepartureTable departures={visibleDepartures} onOpen={openJourney} />}
            </section>
            {journey && <JourneyDetail key={`${journey.journeyInstanceId}-${journey.revision}`} journey={journey} updates={updates} observations={observations} busy={mutating} onRefresh={() => void openJourney(journey.journeyInstanceId)} onPublishStatus={publishStatus} onSaveStop={saveStop} />}
          </>
        )}

        {tab === 'incidents' && <IncidentWorkspace role={role} stations={stations} incidents={visibleIncidents} loading={incidentsLoading} query={incidentQuery} onQuery={setIncidentQuery} state={incidentState} onState={setIncidentState} busy={mutating} onCreate={submitIncident} onTransition={(item) => void transitionIncident(item)} />}
        {tab === 'stations' && role === 'Controller' && <StationManagement stations={stations} loading={stationsLoading} options={options} onRefresh={loadStations} onUnauthorized={onOverviewUnauthorized} />}
        {tab === 'routes' && role === 'Controller' && <RouteManagement options={options} onUnauthorized={onOverviewUnauthorized} />}
        {tab === 'services' && role === 'Controller' && <TrainServiceManagement options={options} onUnauthorized={onOverviewUnauthorized} />}
      </main>
    </div>
  )
}

function SummaryCard({ title, value, hint, loading }: { title: string; value: number; hint: string; loading: boolean }) {
  return <article className="operations-summary"><div className="operations-summary__top"><span>{title}</span><span className="operations-summary__symbol" aria-hidden="true">•</span></div>
    {loading ? <div className="operations-skeleton operations-skeleton--number" aria-label={`Loading ${title}`} /> : <strong>{value}</strong>}
    <small>{hint}</small></article>
}

function PanelHeading({ title, description }: { title: string; description: string }) {
  return <div className="operations__panel-heading"><div><h2>{title}</h2><p>{description}</p></div></div>
}

function SearchInput({ label, value, onChange, placeholder }: { label: string; value: string; onChange: (value: string) => void; placeholder: string }) {
  return <label className="operations-control operations-control--search"><span>{label}</span><span className="operations-search"><SearchIcon /><input type="search" value={value} onChange={(event) => onChange(event.target.value)} placeholder={placeholder} /></span></label>
}

function DepartureFilters({ stations, stationId, setStationId, date, setDate, loading, refresh }: { stations: Station[]; stationId: string; setStationId: (value: string) => void; date: string; setDate: (value: string) => void; loading: boolean; refresh: () => void }) {
  const { t } = useLanguage()
  return <div className="operations__filters"><label className="operations-control"><span>{t('Station')}</span><select value={stationId} onChange={(event) => setStationId(event.target.value)}><option value="">{t('Choose a published station')}</option>{stations.map((station) => <option key={station.stationId} value={station.stationId}>{station.name}</option>)}</select></label>
    <label className="operations-control"><span>{t('Station-local date')}</span><input type="date" value={date} onChange={(event) => setDate(event.target.value)} /></label>
    <button className="button button--primary" type="button" onClick={refresh} disabled={loading || !stationId || !date}><CalendarIcon />{loading ? t('Loading') : t('Load departures')}</button></div>
}

function DataTable<T>({ columns, rows, rowKey, label }: { columns: Column<T>[]; rows: T[]; rowKey: (row: T) => string; label: string }) {
  return <div className="operations__table-wrap"><table className="operations-table"><caption className="sr-only">{label}</caption><thead><tr>{columns.map((column) => <th key={column.key} scope="col">{column.label}</th>)}</tr></thead><tbody>{rows.map((row) => <tr key={rowKey(row)}>{columns.map((column) => <td key={column.key}>{column.render(row)}</td>)}</tr>)}</tbody></table></div>
}

function DepartureTable({ departures, onOpen }: { departures: Departure[]; onOpen: (id: string) => void }) {
  const { t } = useLanguage()
  const columns: Column<Departure>[] = [
    { key: 'departure', label: t('Departure time'), render: (item) => <><b>{timestamp(item.operationalDepartureAt ?? item.scheduledDepartureAt)}</b>{item.ambiguity === 'ambiguous' && <small className="operations-table__note">{t('Ambiguous departure descriptor')}</small>}</> },
    { key: 'service', label: t('Service'), render: (item) => <code>{item.serviceId}</code> },
    { key: 'status', label: t('Operational status'), render: (item) => <StatusBadge value={item.status} /> },
    { key: 'platform', label: t('Platform'), render: (item) => item.platform || t('Unknown') },
    { key: 'updated', label: t('Data updated'), render: (item) => timestamp(item.updatedAt) },
    { key: 'action', label: t('Journey'), render: (item) => <button className="button button--tertiary" type="button" onClick={() => onOpen(item.journeyInstanceId)}>{t('View details')} <span aria-hidden="true">→</span></button> },
  ]
  return <DataTable columns={columns} rows={departures} rowKey={(item) => item.journeyInstanceId} label={t('Station departure board')} />
}

function TableSkeleton() {
  const { t } = useLanguage()
  return <div className="operations-skeleton-table" role="status" aria-label={t('Loading operational data')}><span /><span /><span /><span /></div>
}

function EmptyState({ title, text }: { title: string; text: string }) {
  return <div className="operations-empty"><span className="operations-empty__mark" aria-hidden="true">—</span><h3>{title}</h3><p>{text}</p></div>
}

function StatusBadge({ value }: { value: string }) {
  const { t } = useLanguage()
  const styleKey = value.toLowerCase().replaceAll(' ', '-')
  return <span className={`operations-status operations-status--${styleKey}`}><i aria-hidden="true" />{t(value)}</span>
}

function JourneyDetail({ journey, updates, observations, busy, onRefresh, onPublishStatus, onSaveStop }: { journey: Journey; updates: JourneyUpdate[]; observations: DriverObservation[]; busy: boolean; onRefresh: () => void; onPublishStatus: (event: FormEvent<HTMLFormElement>) => void; onSaveStop: (event: FormEvent<HTMLFormElement>, stopId: string) => void }) {
  const { t } = useLanguage()
  const [statusDraft, setStatusDraft] = useState(journey.status)
  const stopColumns: Column<Journey['stops'][number]>[] = [
    { key: 'station', label: t('Calling point'), render: (stop) => <><b>{stop.stopOrder}. {stop.stationName || stop.stationId}</b><small className="operations-table__note">{stop.stationId}</small></> },
    { key: 'planned', label: t('Planned arrival'), render: (stop) => timestamp(stop.plannedArrivalAt) },
    { key: 'actual', label: t('Operational arrival'), render: (stop) => timestamp(stop.operationalArrivalAt) },
    { key: 'platform', label: t('Platform'), render: (stop) => stop.platform || t('Unknown') },
    { key: 'action', label: t('Operational action'), render: (stop) => <form className="operations-inline-form" onSubmit={(event) => onSaveStop(event, stop.journeyStopId)}><label className="sr-only" htmlFor={`platform-${stop.journeyStopId}`}>{t('Set platform for')} {stop.stationName || `${t('stop')} ${stop.stopOrder}`}</label><input id={`platform-${stop.journeyStopId}`} name="platform" defaultValue={stop.platform ?? ''} required maxLength={30} placeholder={t('Platform')} /><button className="button button--secondary button--small" disabled={busy}>{t('Publish')}</button></form> },
  ]
  return <section className="operations__panel operations__journey-panel" aria-label={t('Selected journey details')}>
    <div className="operations__panel-heading"><div><span className="operations__section-kicker">{t('JOURNEY DETAIL')}</span><h2>{journey.journeyInstanceId}</h2><p>{t('Operation date')} {journey.operationDate} · {t('revision')} {journey.revision}</p></div><button className="button button--secondary" type="button" onClick={onRefresh} disabled={busy}><RefreshIcon />{t('Refresh journey')}</button></div>
    <div className="operations__journey-summary"><div><small>{t('OFFICIAL STATUS')}</small><StatusBadge value={journey.status} /></div><div><small>{t('LAST UPDATED')}</small><b>{timestamp(journey.lastUpdatedAt)}</b></div><div><small>{t('ESTIMATED ARRIVAL')}</small><b>{journey.eta ? timestamp(journey.eta.value) : t('Unknown')}</b></div></div>
    <DataTable columns={stopColumns} rows={journey.stops} rowKey={(stop) => stop.journeyStopId} label={t('Journey calling points and operational times')} />
    <div className="operations__action-box"><div><span className="operations__section-kicker">{t('CONTROLLER ACTION')}</span><h3>{t('Publish official status')}</h3><p>{t('This updates the public operational journey record.')}</p></div><form className="operations__status-form" onSubmit={onPublishStatus}><label className="operations-control"><span>{t('New status')}</span><select name="status" value={statusDraft} onChange={(event) => setStatusDraft(event.target.value)}>{['Scheduled', 'Running', 'Delayed', 'Interrupted', 'Cancelled', 'Completed', 'Terminated'].map((status) => <option key={status} value={status}>{t(status)}</option>)}</select></label><button className="button button--primary" disabled={busy || statusDraft === journey.status}>{t(busy ? 'Publishing…' : 'Publish status')}</button></form></div>
    <div className="operations__detail-columns"><div><h3>Official update history</h3>{updates.length ? <ol className="operations-timeline">{updates.map((item) => <li key={item.updateId}><span className="operations-timeline__dot" /><div><b>{item.kind} · {item.status}</b><small>Revision {item.revision} · {timestamp(item.publishedAt)}</small></div></li>)}</ol> : <EmptyState title="No published updates" text="No official operational updates are recorded for this journey." />}</div>
      <div><h3>Driver observations</h3><p className="operations__muted">Protected evidence. It does not change official journey status.</p>{observations.length ? <ul className="operations-observations">{observations.map((item) => <li key={item.observationId}><p>{item.text}</p><small>{timestamp(item.reportedAtUtc)} · journey revision {item.journeyRevision}</small></li>)}</ul> : <EmptyState title="No observations" text="No driver observations are available for this journey." />}</div></div>
  </section>
}

function StationManagement({ stations, loading, options, onRefresh, onUnauthorized }: { stations: Station[]; loading: boolean; options: { apiBaseUrl: string; accessToken: string | null }; onRefresh: () => Promise<void>; onUnauthorized: () => void }) {
  const [editing, setEditing] = useState<Station | null>(null)
  const [adding, setAdding] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')

  async function saveStation(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = event.currentTarget
    const name = String(new FormData(form).get('name') ?? '').trim()
    if (!name) return
    setBusy(true); setError('')
    try {
      if (editing) await operationsApi.updateStation(editing.stationId, name, options)
      else await operationsApi.createStation(name, options)
      setAdding(false); setEditing(null); setNotice(editing ? 'Station updated.' : 'Station added.')
      await onRefresh()
    } catch (failure) {
      setError(apiErrorMessage(failure))
      if (failure instanceof ApiRequestError && failure.status === 401) onUnauthorized()
    } finally { setBusy(false) }
  }

  async function deleteStation(station: Station) {
    if (!window.confirm(`Delete ${station.name}? Stations used by routes, services, or operational records cannot be deleted.`)) return
    setBusy(true); setError('')
    try {
      await operationsApi.deleteStation(station.stationId, options)
      setNotice('Station deleted.')
      await onRefresh()
    } catch (failure) {
      setError(apiErrorMessage(failure))
      if (failure instanceof ApiRequestError && failure.status === 401) onUnauthorized()
    } finally { setBusy(false) }
  }

  return <section className="operations__panel" aria-label="Station management">
    <div className="operations__panel-heading"><div><span className="operations__section-kicker">RAILWAY NETWORK</span><h2>Stations</h2><p>Create, update, and remove railway stations.</p></div><button className="button button--primary" type="button" onClick={() => { setEditing(null); setAdding(true); setError('') }}>Add Station</button></div>
    {error && <div className="operations__feedback operations__feedback--error" role="alert">{error}</div>}
    {notice && <div className="operations__feedback" role="status">{notice}</div>}
    {(adding || editing) && <form className="operations-station-form" onSubmit={(event) => void saveStation(event)}>
      <label className="operations-control"><span>Station name</span><input autoFocus name="name" required maxLength={200} defaultValue={editing?.name ?? ''} placeholder="Enter station name" /></label>
      <div className="operations-station-form__actions"><button className="button button--primary" type="submit" disabled={busy}>{busy ? 'Saving…' : editing ? 'Save changes' : 'Add station'}</button><button className="button button--secondary" type="button" disabled={busy} onClick={() => { setAdding(false); setEditing(null); setError('') }}>Cancel</button></div>
    </form>}
    {loading ? <TableSkeleton /> : stations.length === 0 ? <EmptyState title="No stations found" text="Add a station to begin managing the railway network." /> : <DataTable label="Stations" columns={[
      { key: 'name', label: 'Station', render: (station: Station) => <><b>{station.name}</b><small className="operations-table__note">{station.stationId}</small></> },
      { key: 'details', label: 'Details', render: (station: Station) => station.locationDetails?.join(' · ') || '—' },
      { key: 'actions', label: 'Actions', render: (station: Station) => <div className="operations-station-actions"><button className="button button--secondary button--small" type="button" disabled={busy} onClick={() => { setAdding(false); setEditing(station); setError('') }}>Edit</button><button className="button button--secondary button--small" type="button" disabled={busy} onClick={() => void deleteStation(station)}>Delete</button></div> },
    ]} rows={stations} rowKey={(station: Station) => station.stationId} />}
  </section>
}

function IncidentWorkspace({ role, stations, incidents, loading, query, onQuery, state, onState, busy, onCreate, onTransition }: { role: string; stations: Station[]; incidents: Incident[]; loading: boolean; query: string; onQuery: (value: string) => void; state: string; onState: (value: string) => void; busy: boolean; onCreate: (event: FormEvent<HTMLFormElement>) => void; onTransition: (incident: Incident) => void }) {
  const { t } = useLanguage()
  return <section className="operations__panel"><PanelHeading title={t('Incident management')} description={t('The backend applies role grants, current station scope, and forward-only lifecycle rules.')} />
    <div className="operations__table-tools"><SearchInput label={t('Search incidents')} value={query} onChange={onQuery} placeholder={t('Description, category, incident ID…')} /><label className="operations-control"><span>{t('Lifecycle state')}</span><select value={state} onChange={(event) => onState(event.target.value)}><option value="all">{t('All states')}</option>{['Reported', 'In Progress', 'Resolved', 'Closed'].map((item) => <option key={item} value={item}>{t(item)}</option>)}</select></label></div>
    <div className="operations__incident-grid"><div><h3>{t('Authorized incidents')} <span className="operations__count">{incidents.length}</span></h3>{loading ? <IncidentSkeleton /> : incidents.length ? <ul className="operations-incident-list">{incidents.map((item) => <li key={item.incidentId}><div className="operations-incident-list__main"><StatusBadge value={item.state} /><span className="operations-incident-list__meta">{item.categoryCode} · {t('revision')} {item.revision}</span><h4>{item.description}</h4><small>{t('Reported')} {timestamp(item.reportedAt)}{item.occurredAt ? ` · ${t('Occurred')} ${timestamp(item.occurredAt)}` : ''}</small><small>{item.associations.map((association) => `${t(association.kind)}: ${association.resourceId}`).join(' · ')}</small></div>{item.state !== 'Closed' && <button className="button button--secondary button--small" type="button" disabled={busy} onClick={() => onTransition(item)}>{t('Advance to')} {t(item.state === 'Reported' ? 'In Progress' : item.state === 'In Progress' ? 'Resolved' : 'Closed')}</button>}</li>)}</ul> : <EmptyState title={t('No incidents match')} text={t('There are no incidents in the selected scope and filter.')} />}</div>
      <div className="operations__report-card"><span className="operations__section-kicker">{t('OPERATIONAL ACTION')}</span><h3>{t('Report an incident')}</h3><p>{t('Station association is checked against your current backend permissions.')}</p>
        <form className="operations-form" onSubmit={onCreate}><label className="operations-control"><span>{t('Approved category code')}</span><input name="categoryCode" required maxLength={80} placeholder={t('Enter category code')} /></label><small className="operations-form__hint">{t('The backend checks this against its controlled catalogue.')}</small><label className="operations-control"><span>{t('Incident description')}</span><textarea name="description" required minLength={1} maxLength={4000} rows={4} placeholder={t('Describe the operational issue')} /></label><label className="operations-control"><span>{t('Related station')}</span>{stations.length ? <select name="resourceId" required>{stations.map((station) => <option key={station.stationId} value={station.stationId}>{station.name}</option>)}</select> : <input name="resourceId" required placeholder={t('Station UUID')} />}</label><input type="hidden" name="kind" value="Station" /><p className="operations-form__scope">{t(role === 'Station Staff' ? 'Your station assignment is verified by the server. If it denies this report, the selected station is outside your current scope.' : 'The server validates this station reference and your Controller permission.')}</p><button className="button button--primary" type="submit" disabled={busy || !stations.length}>{t(busy ? 'Submitting report…' : 'Submit incident report')}</button></form>
      </div></div>
  </section>
}

function IncidentSkeleton() {
  return <div className="operations-skeleton-list" role="status" aria-label="Loading incidents"><span /><span /><span /></div>
}

function RefreshIcon() { return <svg aria-hidden="true" viewBox="0 0 20 20"><path d="M16.5 7A7 7 0 0 0 4.3 4.8L3 6m0 0V2.8M3 6h3.2M3.5 13a7 7 0 0 0 12.2 2.2L17 14m0 0v3.2M17 14h-3.2" /></svg> }
function SearchIcon() { return <svg aria-hidden="true" viewBox="0 0 20 20"><circle cx="8.7" cy="8.7" r="5.5" /><path d="m13 13 4 4" /></svg> }
function CalendarIcon() { return <svg aria-hidden="true" viewBox="0 0 20 20"><rect x="2.5" y="4" width="15" height="13" rx="2" /><path d="M6 2.5v3M14 2.5v3M3 8h14" /></svg> }
function StationIcon() { return <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M5 20V6l7-3 7 3v14M8 20v-5h8v5M8 8h2m4 0h2M8 11h2m4 0h2M3 21h18" /></svg> }
function SidebarIcon({ name }: { name: string }) {
  const paths: Record<string, ReactNode> = {
    dashboard: <><rect x="3" y="3" width="7" height="7" rx="1" /><rect x="14" y="3" width="7" height="5" rx="1" /><rect x="14" y="12" width="7" height="9" rx="1" /><rect x="3" y="14" width="7" height="7" rx="1" /></>,
    train: <><path d="M7 18h10M8 21l2-3m6 3-2-3M7 15l-2 3m12-3 2 3" /><rect x="6" y="3" width="12" height="14" rx="5" /><path d="M9 7h6M9 11h.01M15 11h.01" /></>,
    clock: <><circle cx="12" cy="12" r="9" /><path d="M12 7v5l3 2" /></>,
    signal: <><path d="M12 21v-5m-4 5h8M8 12a5.7 5.7 0 0 1 0-8m8 0a5.7 5.7 0 0 1 0 8M5 15a9 9 0 0 1 0-13m14 0a9 9 0 0 1 0 13M12 13a2 2 0 1 0 0-4 2 2 0 0 0 0 4Z" /></>,
    alert: <><path d="m12 3 9 16H3L12 3Z" /><path d="M12 9v4m0 3h.01" /></>,
    people: <><path d="M16 20v-1.5a4 4 0 0 0-4-4H7a4 4 0 0 0-4 4V20m4-9a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7Zm9-6a3.5 3.5 0 0 1 0 6m1 3.5a4 4 0 0 1 4 4V20" /></>,
    pin: <><path d="M20 10c0 5-8 11-8 11S4 15 4 10a8 8 0 1 1 16 0Z" /><circle cx="12" cy="10" r="2.5" /></>,
    route: <><circle cx="6" cy="6" r="2.5" /><circle cx="18" cy="18" r="2.5" /><path d="M8.5 6H13a5 5 0 0 1 0 10h-2a3.5 3.5 0 0 0-3.5 3.5" /></>,
    calendar: <><rect x="3" y="5" width="18" height="16" rx="2" /><path d="M7 3v4m10-4v4M3 10h18m-9 3v4l3 1" /><circle cx="12" cy="16" r="4" /></>,
    profile: <><circle cx="12" cy="12" r="9" /><circle cx="12" cy="9" r="3" /><path d="M6.5 19a6 6 0 0 1 11 0" /></>,
  }
  return <svg className="operations-menu__icon" aria-hidden="true" viewBox="0 0 24 24">{paths[name]}</svg>
}
