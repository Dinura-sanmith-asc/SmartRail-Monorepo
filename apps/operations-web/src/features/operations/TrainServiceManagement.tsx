import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { ApiRequestError } from '../../shared/api/fetchJson'
import { apiErrorMessage, operationsApi, type ManagedRoute, type ManagedTrainService, type OperationsApiOptions } from './operationsApi'

type Props = { options: OperationsApiOptions; onUnauthorized: () => void }

function problem(error: unknown, onUnauthorized: () => void) {
  if (error instanceof ApiRequestError && error.status === 401) onUnauthorized()
  return apiErrorMessage(error)
}

export function TrainServiceManagement({ options, onUnauthorized }: Props) {
  const [services, setServices] = useState<ManagedTrainService[]>([])
  const [routes, setRoutes] = useState<ManagedRoute[]>([])
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [editing, setEditing] = useState<ManagedTrainService | null>(null)
  const [adding, setAdding] = useState(false)
  const [query, setQuery] = useState('')

  const visibleServices = useMemo(() => {
    const search = query.trim().toLocaleLowerCase()
    if (!search) return services
    return services.filter((service) =>
      (service.publicServiceNumber ?? '').toLocaleLowerCase().includes(search) ||
      (service.displayName ?? '').toLocaleLowerCase().includes(search),
    )
  }, [query, services])

  const load = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const [serviceResult, routeResult] = await Promise.allSettled([
        operationsApi.managedServices(options),
        operationsApi.managedRoutes(options),
      ])
      const failures: string[] = []
      if (serviceResult.status === 'fulfilled') setServices(serviceResult.value)
      else failures.push(problem(serviceResult.reason, onUnauthorized))
      if (routeResult.status === 'fulfilled') setRoutes(routeResult.value)
      else failures.push(problem(routeResult.reason, onUnauthorized))
      if (failures.length) setError([...new Set(failures)].join(' '))
    } catch (failure) {
      setError(problem(failure, onUnauthorized))
    } finally {
      setLoading(false)
    }
  }, [onUnauthorized, options])

  useEffect(() => { void load() }, [load])

  function clearEditor() {
    setAdding(false)
    setEditing(null)
    setError('')
  }

  async function saveService(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const values = new FormData(event.currentTarget)
    const body = {
      serviceCode: String(values.get('serviceCode') ?? '').trim(),
      routeId: String(values.get('routeId') ?? ''),
      displayName: String(values.get('displayName') ?? '').trim() || null,
      publicServiceNumber: String(values.get('publicServiceNumber') ?? '').trim() || null,
    }
    setBusy(true)
    setError('')
    try {
      if (editing) await operationsApi.updateTrainService(editing.serviceId, body, options)
      else await operationsApi.createTrainService(body, options)
      setNotice(editing ? 'Train service updated.' : 'Train service added.')
      clearEditor()
      await load()
    } catch (failure) {
      setError(problem(failure, onUnauthorized))
    } finally {
      setBusy(false)
    }
  }

  async function deleteService(service: ManagedTrainService) {
    const label = [service.publicServiceNumber, service.displayName].filter(Boolean).join(' · ') || service.serviceCode
    if (!window.confirm(`Delete ${label}? Services referenced by operational records cannot be deleted.`)) return
    setBusy(true)
    setError('')
    try {
      await operationsApi.deleteTrainService(service.serviceId, options)
      setNotice('Train service deleted.')
      await load()
    } catch (failure) {
      setError(problem(failure, onUnauthorized))
    } finally {
      setBusy(false)
    }
  }

  return <section className="operations__panel train-service-panel" aria-label="Train service management">
    <div className="operations__panel-heading"><div><span className="operations__section-kicker">RAILWAY NETWORK</span><h2>Train Services</h2><p>Add, edit, search, and remove train services.</p></div><button className="button button--primary" type="button" onClick={() => { setEditing(null); setAdding(true); setError('') }}>Add Train Service</button></div>
    {error && <div className="operations__feedback operations__feedback--error" role="alert">{error}</div>}
    {notice && <div className="operations__feedback" role="status">{notice}</div>}
    {(adding || editing) && <form className="operations-form train-service-form" onSubmit={(event) => void saveService(event)}>
      <label className="operations-control"><span>Service code</span><input name="serviceCode" required maxLength={50} defaultValue={editing?.serviceCode ?? ''} /></label>
      <label className="operations-control"><span>Route</span><select name="routeId" required defaultValue={editing?.routeId ?? ''}><option value="" disabled>Select a route</option>{routes.map((route) => <option key={route.routeId} value={route.routeId}>{route.routeCode} · {route.name}{route.isPublished ? '' : ' (unpublished)'}</option>)}</select></label>
      <label className="operations-control"><span>Display name</span><input name="displayName" maxLength={200} defaultValue={editing?.displayName ?? ''} /></label>
      <label className="operations-control"><span>Public service number</span><input name="publicServiceNumber" maxLength={50} defaultValue={editing?.publicServiceNumber ?? ''} /></label>
      <div className="operations-station-form__actions"><button className="button button--primary" type="submit" disabled={busy || routes.length === 0}>{busy ? 'Saving…' : editing ? 'Save changes' : 'Add service'}</button><button className="button button--secondary" type="button" disabled={busy} onClick={clearEditor}>Cancel</button></div>
    </form>}
    <label className="operations-control train-service-search"><span>Search train services</span><span className="operations-search"><input type="search" value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Public number or display name" /></span></label>
    {loading ? <div className="operations-skeleton-list" role="status" aria-label="Loading train services"><span /><span /><span /></div> : visibleServices.length === 0 ? <div className="operations-empty"><b>{services.length === 0 ? 'No train services found' : 'No matching train services'}</b><span>{services.length === 0 ? 'Add a train service to begin.' : 'Try another public number or display name.'}</span></div> : <div className="operations__table-wrap"><table className="operations-table"><caption className="sr-only">Train services</caption><thead><tr><th>Public number</th><th>Display name</th><th>Route code</th><th>Actions</th></tr></thead><tbody>{visibleServices.map((service) => <tr key={service.serviceId}><td>{service.publicServiceNumber || '—'}</td><td>{service.displayName || '—'}</td><td>{service.routeCode}</td><td><div className="operations-station-actions"><button className="button button--secondary button--small" type="button" disabled={busy} onClick={() => { setEditing(service); setAdding(false); setError('') }}>Edit</button><button className="button button--secondary button--small" type="button" disabled={busy} onClick={() => void deleteService(service)}>Delete</button></div></td></tr>)}</tbody></table></div>}
  </section>
}
