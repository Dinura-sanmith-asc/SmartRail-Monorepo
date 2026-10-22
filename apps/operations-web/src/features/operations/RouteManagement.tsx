import { Fragment, useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { ApiRequestError } from '../../shared/api/fetchJson'
import { apiErrorMessage, operationsApi, type ManagedRoute, type OperationsApiOptions, type Station } from './operationsApi'

type Props = { options: OperationsApiOptions; onUnauthorized: () => void }
type RouteDraft = { routeStopId: string; stationId: string; stationName: string }

function problem(error: unknown, onUnauthorized: () => void) {
  if (error instanceof ApiRequestError && error.status === 401) onUnauthorized()
  return apiErrorMessage(error)
}

export function RouteManagement({ options, onUnauthorized }: Props) {
  const [routes, setRoutes] = useState<ManagedRoute[]>([])
  const [stations, setStations] = useState<Station[]>([])
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [editing, setEditing] = useState<ManagedRoute | null>(null)
  const [adding, setAdding] = useState(false)
  const [query, setQuery] = useState('')
  const [expandedId, setExpandedId] = useState('')
  const [draftStops, setDraftStops] = useState<RouteDraft[]>([])
  const [stationToAdd, setStationToAdd] = useState('')

  const visibleRoutes = useMemo(() => {
    const search = query.trim().toLocaleLowerCase()
    if (!search) return routes
    return routes.filter((route) => route.routeCode.toLocaleLowerCase().includes(search) || route.name.toLocaleLowerCase().includes(search))
  }, [query, routes])
  const availableStations = stations.filter((station) => !draftStops.some((stop) => stop.stationId === station.stationId))

  const load = useCallback(async () => {
    setLoading(true)
    setError('')
    const [routeResult, stationResult] = await Promise.allSettled([
      operationsApi.managedRoutes(options),
      operationsApi.stations(options),
    ])
    const failures: string[] = []
    if (routeResult.status === 'fulfilled') setRoutes(routeResult.value)
    else failures.push(problem(routeResult.reason, onUnauthorized))
    if (stationResult.status === 'fulfilled') setStations(stationResult.value.items)
    else failures.push(problem(stationResult.reason, onUnauthorized))
    if (failures.length) setError([...new Set(failures)].join(' '))
    setLoading(false)
  }, [onUnauthorized, options])

  useEffect(() => { void load() }, [load])

  function clearEditor() {
    setAdding(false)
    setEditing(null)
    setDraftStops([])
    setStationToAdd('')
    setError('')
  }

  function startAdd() {
    setEditing(null)
    setDraftStops([])
    setStationToAdd('')
    setAdding(true)
    setError('')
  }

  function startEdit(route: ManagedRoute) {
    setAdding(false)
    setEditing(route)
    setDraftStops(route.stops.map((stop) => ({ routeStopId: stop.routeStopId, stationId: stop.stationId, stationName: stop.stationName })))
    setStationToAdd('')
    setError('')
  }

  function addDraftStop() {
    const station = stations.find((item) => item.stationId === stationToAdd)
    if (!station) return
    setDraftStops((current) => [...current, { routeStopId: `new-${crypto.randomUUID()}`, stationId: station.stationId, stationName: station.name }])
    setStationToAdd('')
  }

  async function saveRoute(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const values = new FormData(event.currentTarget)
    const body = {
      routeCode: String(values.get('routeCode') ?? '').trim(),
      name: String(values.get('name') ?? '').trim(),
      stationIds: draftStops.map((stop) => stop.stationId),
    }
    setBusy(true)
    setError('')
    try {
      if (editing) await operationsApi.updateRoute(editing.routeId, body, options)
      else await operationsApi.createRoute(body, options)
      setNotice(editing ? 'Route updated.' : 'Route added.')
      clearEditor()
      await load()
    } catch (failure) {
      setError(problem(failure, onUnauthorized))
    } finally {
      setBusy(false)
    }
  }

  async function deleteRoute(route: ManagedRoute) {
    if (!window.confirm(`Delete route ${route.routeCode}? Routes referenced by train services or schedules cannot be deleted.`)) return
    setBusy(true)
    setError('')
    try {
      await operationsApi.deleteRoute(route.routeId, options)
      setNotice('Route deleted.')
      if (expandedId === route.routeId) setExpandedId('')
      await load()
    } catch (failure) {
      setError(problem(failure, onUnauthorized))
    } finally {
      setBusy(false)
    }
  }

  return <section className="operations__panel route-management-panel" aria-label="Route management">
    <div className="operations__panel-heading"><div><span className="operations__section-kicker">RAILWAY NETWORK</span><h2>Routes</h2><p>Add and maintain railway routes and their ordered station stops.</p></div><button className="button button--primary" type="button" onClick={startAdd}>Add Route</button></div>
    {error && <div className="operations__feedback operations__feedback--error" role="alert">{error}</div>}
    {notice && <div className="operations__feedback" role="status">{notice}</div>}
    {(adding || editing) && <form className="operations-form route-management-form" onSubmit={(event) => void saveRoute(event)}>
      <label className="operations-control"><span>Route code</span><input name="routeCode" required maxLength={50} defaultValue={editing?.routeCode ?? ''} placeholder="Enter route code" /></label>
      <label className="operations-control"><span>Route name</span><input name="name" required maxLength={200} defaultValue={editing?.name ?? ''} placeholder="Enter route name" /></label>
      <fieldset className="route-stop-editor"><legend>Stations on this route</legend>
        {draftStops.length ? <ol className="route-stop-editor__list">{draftStops.map((stop, index) => <li key={stop.routeStopId}>
          <span><b>{index + 1}.</b> {stop.stationName}</span>
          <button className="button button--secondary button--small" type="button" disabled={busy} aria-label={`Remove ${stop.stationName} from route`} onClick={() => setDraftStops((current) => current.filter((item) => item.routeStopId !== stop.routeStopId))}>Remove</button>
        </li>)}</ol> : <p className="route-stop-editor__empty">No stations added yet.</p>}
        <div className="route-stop-editor__add"><label className="operations-control"><span>Add station</span><select value={stationToAdd} onChange={(event) => setStationToAdd(event.target.value)}><option value="">Select a station</option>{availableStations.map((station) => <option key={station.stationId} value={station.stationId}>{station.name}</option>)}</select></label><button className="button button--secondary" type="button" disabled={busy || !stationToAdd} onClick={addDraftStop}>Add station</button></div>
      </fieldset>
      <div className="operations-station-form__actions"><button className="button button--primary" type="submit" disabled={busy}>{busy ? 'Saving…' : editing ? 'Save changes' : 'Add route'}</button><button className="button button--secondary" type="button" disabled={busy} onClick={clearEditor}>Cancel</button></div>
    </form>}
    <label className="operations-control route-management-search"><span>Search routes</span><span className="operations-search"><input type="search" value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Route code" /></span></label>
    {loading ? <div className="operations-skeleton-list" role="status" aria-label="Loading routes"><span /><span /><span /></div> : visibleRoutes.length === 0 ? <div className="operations-empty"><b>{routes.length === 0 ? 'No routes found' : 'No matching routes'}</b><span>{routes.length === 0 ? 'Add a route to begin managing railway paths.' : 'Try another route code.'}</span></div> : <div className="operations__table-wrap"><table className="operations-table"><caption className="sr-only">Routes</caption><thead><tr><th>Route code</th><th>Route name</th><th>Actions</th></tr></thead><tbody>{visibleRoutes.map((route) => <Fragment key={route.routeId}>
      <tr key={route.routeId}><td><button className="route-code-button" type="button" aria-expanded={expandedId === route.routeId} onClick={() => setExpandedId((current) => current === route.routeId ? '' : route.routeId)}>{route.routeCode}<span aria-hidden="true">{expandedId === route.routeId ? '⌄' : '›'}</span></button></td><td>{route.name}</td><td><div className="operations-station-actions"><button className="button button--secondary button--small" type="button" disabled={busy} onClick={() => startEdit(route)}>Edit</button><button className="button button--secondary button--small" type="button" disabled={busy} onClick={() => void deleteRoute(route)}>Delete</button></div></td></tr>
      {expandedId === route.routeId && <tr key={`${route.routeId}-stops`}><td colSpan={3}><div className="route-stops-detail"><b>Stations on {route.routeCode}</b>{route.stops.length ? <ol>{route.stops.map((stop) => <li key={stop.routeStopId}>{stop.stationName}</li>)}</ol> : <span>No stations are assigned to this route.</span>}</div></td></tr>}
    </Fragment>)}</tbody></table></div>}
  </section>
}
