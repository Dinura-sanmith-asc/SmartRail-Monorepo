import { useEffect, useMemo, useState } from 'react'
import { ApiRequestError } from '../../shared/api/fetchJson'
import { useLanguage } from '../../shared/i18n/language'
import { apiErrorMessage, operationsApi, type OperationsApiOptions, type OperationsOverview } from './operationsApi'
import { colomboToday, filterSortOverviewJourneys, type JourneyFilter, type SortDirection, type SortKey } from './controllerOverviewModel'

function time(value: string | null) {
  if (!value) return 'Unknown'
  return new Intl.DateTimeFormat('en-LK', { timeZone: 'Asia/Colombo', dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))
}

const sortColumns: { key: SortKey; label: string }[] = [
  { key: 'service', label: 'Train name / number' },
  { key: 'route', label: 'Origin → destination' },
  { key: 'departure', label: 'Scheduled departure' },
  { key: 'arrival', label: 'Scheduled arrival' },
  { key: 'status', label: 'Journey status' },
  { key: 'delay', label: 'Delay' },
  { key: 'station', label: 'Last reported station' },
  { key: 'updated', label: 'Last updated' },
]

export function ControllerOverview({ options, onOpenJourney, onOpenIncidents, onUnauthorized }: {
  options: OperationsApiOptions
  onOpenJourney: (journeyId: string) => void
  onOpenIncidents: () => void
  onUnauthorized: () => void
}) {
  const { t } = useLanguage()
  const [date, setDate] = useState(colomboToday)
  const [overview, setOverview] = useState<OperationsOverview | null>(null)
  const [lastUpdated, setLastUpdated] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [refreshing, setRefreshing] = useState(false)
  const [reload, setReload] = useState(0)
  const [query, setQuery] = useState('')
  const [filter, setFilter] = useState<JourneyFilter>('all')
  const [sortKey, setSortKey] = useState<SortKey>('departure')
  const [direction, setDirection] = useState<SortDirection>('ascending')

  useEffect(() => {
    let active = true
    let inFlight = false
    let controller: AbortController | null = null
    const refresh = async () => {
      if (inFlight) return
      inFlight = true
      controller = new AbortController()
      setRefreshing(true)
      try {
        const result = await operationsApi.operationsOverview(date, { ...options, signal: controller.signal })
        if (result.operationalDate !== date) throw new ApiRequestError('malformed-response')
        if (active) {
          setOverview(result)
          setLastUpdated(new Date().toISOString())
          setError(null)
        }
      } catch (failure) {
        if (active && !controller.signal.aborted) {
          setError(apiErrorMessage(failure))
          if (failure instanceof ApiRequestError && failure.status === 401) onUnauthorized()
        }
      } finally {
        inFlight = false
        if (active) {
          setLoading(false)
          setRefreshing(false)
        }
      }
    }
    void refresh()
    const interval = window.setInterval(() => void refresh(), 30_000)
    return () => {
      active = false
      controller?.abort()
      window.clearInterval(interval)
    }
  }, [date, options, reload, onUnauthorized])

  const visibleJourneys = useMemo(() => filterSortOverviewJourneys(overview?.journeys ?? [], query, filter, sortKey, direction),
    [overview?.journeys, query, filter, sortKey, direction])

  function chooseDate(value: string) {
    if (!value) return
    setOverview(null)
    setLastUpdated(null)
    setError(null)
    setLoading(true)
    setFilter('all')
    setDate(value)
  }

  function chooseSort(key: SortKey) {
    if (sortKey === key) setDirection((current) => current === 'ascending' ? 'descending' : 'ascending')
    else { setSortKey(key); setDirection('ascending') }
  }

  const metricCards: { key: keyof OperationsOverview['metrics']; label: string; filter?: JourneyFilter; current?: boolean }[] = [
    { key: 'totalScheduled', label: date === colomboToday() ? 'Scheduled Journeys Today' : 'Scheduled Journeys', filter: 'all' },
    { key: 'running', label: 'Currently Running', filter: 'Running' },
    { key: 'delayed', label: 'Delayed Journeys', filter: 'delayed' },
    { key: 'openIncidents', label: 'Open Incidents', current: true },
    { key: 'cancelled', label: 'Cancelled Journeys', filter: 'Cancelled' },
    { key: 'completed', label: 'Completed Journeys', filter: 'Completed' },
  ]

  return <section className="control-centre" aria-labelledby="control-centre-heading">
    <div className="control-centre__heading">
      <div><span className="operations__section-kicker">RAILWAY CONTROL CENTRE</span><h2 id="control-centre-heading">Operational overview and live journey status</h2></div>
      <div className="control-centre__refresh-tools">
        <label className="operations-control" htmlFor="control-centre-date"><span>Operating date (Asia/Colombo)</span>
          <input id="control-centre-date" type="date" value={date} onChange={(event) => chooseDate(event.target.value)} />
        </label>
        <button className="button button--secondary" type="button" onClick={() => setReload((value) => value + 1)} disabled={refreshing}>
          <span aria-hidden="true">↻</span>{refreshing ? 'Refreshing' : 'Refresh'}
        </button>
      </div>
    </div>

    {error && <div className={`operations__feedback ${overview ? 'operations__feedback--stale' : 'operations__feedback--error'}`} role="alert">
      <span><b>{overview ? 'Showing stale overview data' : 'Overview unavailable'}</b><small>{t(error)}{overview && lastUpdated ? ` Last successful update: ${time(lastUpdated)}.` : ''}</small></span>
      <button className="button button--secondary" type="button" onClick={() => setReload((value) => value + 1)}>{t('Retry request')}</button>
    </div>}

    <div className="control-centre__metrics" aria-label="Railway operations metrics">
      {metricCards.map((card) => {
        const content = <><span className="control-centre__metric-label">{card.label}{card.current ? ' — Current' : ''}</span>
          {loading && !overview ? <span className="operations-skeleton operations-skeleton--number" aria-label={`Loading ${card.label}`} /> : <strong>{overview?.metrics[card.key] ?? '—'}</strong>}
          {card.current ? <small>Reported and In Progress incidents, independent of the selected journey date.</small> : <small>{card.key === 'totalScheduled' ? `For ${date}` : 'Selected operating date'}</small>}</>
        return <button key={card.key} type="button" className="control-centre__metric" onClick={() => card.current ? onOpenIncidents() : setFilter(card.filter ?? 'all')} disabled={!overview && !card.current} aria-pressed={card.filter ? filter === card.filter : undefined} aria-label={card.current ? `${card.label} — Current; open Incident Management` : `${card.label}: ${overview?.metrics[card.key] ?? 'unavailable'}. Filter journey board`}>
          {content}
        </button>
      })}
    </div>

    <section className="operations__panel control-centre__board" aria-labelledby="live-journey-board-heading">
      <div className="operations__panel-heading">
        <div><span className="operations__section-kicker">{!error && lastUpdated ? 'LIVE · REFRESHES EVERY 30 SECONDS' : overview && error ? 'STALE DATA' : 'LIVE JOURNEY BOARD'}</span>
          <h2 id="live-journey-board-heading">Live Journey Board</h2>
          <p>{overview ? `${overview.journeys.length} journeys · Operating date ${overview.operationalDate} (Asia/Colombo)` : `Journey data for ${date} (Asia/Colombo)`}</p></div>
        {lastUpdated && <span className="control-centre__updated" role="status">Last successful update: {time(lastUpdated)}</span>}
      </div>
      <div className="operations__table-tools control-centre__filters">
        <label className="operations-control operations-control--search"><span>Search journeys</span><span className="operations-search"><input type="search" value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Train, number, service code, route…" /></span></label>
        <label className="operations-control"><span>Journey status</span><select value={filter} onChange={(event) => setFilter(event.target.value as JourneyFilter)}>
          <option value="all">All journeys</option><option value="delayed">Delayed journeys</option>
          {['Scheduled', 'Running', 'Delayed', 'Interrupted', 'Cancelled', 'Terminated', 'Completed'].map((status) => <option key={status} value={status}>{status}</option>)}
        </select></label>
      </div>
      {loading && !overview ? <TableSkeleton /> : overview && visibleJourneys.length > 0 ? <div className="operations__table-wrap">
        <table className="operations-table control-centre__table"><caption className="sr-only">Live journey board for {overview.operationalDate}; lifecycle status and recorded delay are shown separately.</caption>
          <thead><tr>{sortColumns.map((column) => <th key={column.key} scope="col" aria-sort={sortKey === column.key ? direction : 'none'}><button type="button" onClick={() => chooseSort(column.key)}>{column.label}<span aria-hidden="true">{sortKey === column.key ? direction === 'ascending' ? ' ↑' : ' ↓' : ''}</span></button></th>)}<th scope="col">Actions</th></tr></thead>
          <tbody>{visibleJourneys.map((journey) => <tr key={journey.journeyId}>
            <td><b>{journey.serviceDisplayName || 'Service name unavailable'}</b><small className="operations-table__note">Train No: {journey.publicServiceNumber || 'Train number unavailable'}</small><small className="operations-table__note">Service code: {journey.serviceCode}</small><small className="operations-table__note">Service ID: <code>{journey.serviceId}</code></small></td>
            <td><b>{journey.origin} → {journey.destination}</b><small className="operations-table__note">{journey.routeName} · {journey.routeCode}</small></td>
            <td>{time(journey.scheduledDeparture)}</td><td>{time(journey.scheduledArrival)}</td>
            <td><StatusBadge value={journey.status} /></td>
            <td>{journey.recordedDelayMinutes === null ? <span className="control-centre__unknown">Delay unknown</span> : journey.recordedDelayMinutes === 0 ? 'On time' : `${journey.recordedDelayMinutes} min`}</td>
            <td>{journey.lastReportedStation ?? <span className="control-centre__unknown">Not reported</span>}</td>
            <td>{time(journey.lastUpdateAt)}</td>
            <td><button className="button button--tertiary" type="button" onClick={() => onOpenJourney(journey.journeyId)}>View details <span aria-hidden="true">→</span></button></td>
          </tr>)}</tbody>
        </table>
      </div> : overview ? <EmptyState title={overview.journeys.length ? 'No matching journeys' : 'No journeys for this operating date'} text={overview.journeys.length ? 'Try changing the search or status filter.' : 'The backend returned no journeys for the selected date.'} /> : !error ? <TableSkeleton /> : null}
    </section>
  </section>
}

function StatusBadge({ value }: { value: string }) {
  const styleKey = value.toLowerCase().replaceAll(' ', '-')
  return <span className={`operations-status operations-status--${styleKey}`}><i aria-hidden="true" />{value}</span>
}

function TableSkeleton() {
  return <div className="operations-skeleton-table" role="status" aria-label="Loading operational journeys"><span /><span /><span /><span /></div>
}

function EmptyState({ title, text }: { title: string; text: string }) {
  return <div className="operations-empty"><span className="operations-empty__mark" aria-hidden="true">—</span><h3>{title}</h3><p>{text}</p></div>
}
