import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { ApiRequestError } from '../../shared/api/fetchJson'
import { apiErrorMessage, operationsApi, type ManagedRoute, type ManagedSchedule, type ManagedTrainService, type OperationsApiOptions } from './operationsApi'

type Props = { options: OperationsApiOptions; onUnauthorized: () => void }
type CallDraft = { routeStopId: string; arrivalOffsetMinutes: number | null; departureOffsetMinutes: number | null }

function problem(error: unknown, onUnauthorized: () => void) {
  if (error instanceof ApiRequestError && error.status === 401) onUnauthorized()
  return apiErrorMessage(error)
}

function timeValue(value: string) { return value.slice(0, 5) }

export function TrainServiceManagement({ options, onUnauthorized }: Props) {
  const [services, setServices] = useState<ManagedTrainService[]>([])
  const [routes, setRoutes] = useState<ManagedRoute[]>([])
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [editing, setEditing] = useState<ManagedTrainService | null>(null)
  const [adding, setAdding] = useState(false)
  const [selectedId, setSelectedId] = useState('')
  const [schedules, setSchedules] = useState<ManagedSchedule[]>([])
  const [manifestSchedules, setManifestSchedules] = useState<{ serviceId: string; label: string; schedule: ManagedSchedule }[]>([])
  const [scheduleId, setScheduleId] = useState('')
  const [editingSchedule, setEditingSchedule] = useState(false)
  const [departureTime, setDepartureTime] = useState('')
  const [calls, setCalls] = useState<CallDraft[]>([])
  const [operationDates, setOperationDates] = useState<string[]>([])
  const [newOperationDate, setNewOperationDate] = useState('')
  const [manifestDate, setManifestDate] = useState('')
  const [manifestSource, setManifestSource] = useState('')
  const [manifestFixture, setManifestFixture] = useState('')
  const [manifestHash, setManifestHash] = useState('')
  const [manifestComplete, setManifestComplete] = useState(true)
  const [manifestScheduleIds, setManifestScheduleIds] = useState<string[]>([])

  const selectedService = services.find((service) => service.serviceId === selectedId) ?? null
  const selectedRoute = routes.find((route) => route.routeId === selectedService?.routeId)
  const selectedSchedule = schedules.find((schedule) => schedule.scheduleId === scheduleId) ?? null
  const publishedSchedules = useMemo(() => manifestSchedules.filter((item) => item.schedule.isPublished && !item.schedule.supersededAt), [manifestSchedules])

  const load = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const [serviceItems, routeItems] = await Promise.all([operationsApi.managedServices(options), operationsApi.managedRoutes(options)])
      const serviceSchedules = await Promise.all(serviceItems.map(async (service) => ({ service, schedules: await operationsApi.serviceSchedules(service.serviceId, options) })))
      setServices(serviceItems)
      setRoutes(routeItems)
      setManifestSchedules(serviceSchedules.flatMap(({ service, schedules: items }) => items.map((schedule) => ({
        serviceId: service.serviceId,
        label: `${schedule.revision} · ${service.publicServiceNumber ? `${service.publicServiceNumber} · ` : ''}${service.displayName || service.serviceCode}`,
        schedule,
      }))))
      setSelectedId((current) => current && serviceItems.some((item) => item.serviceId === current) ? current : '')
    } catch (failure) {
      setError(problem(failure, onUnauthorized))
    } finally { setLoading(false) }
  }, [onUnauthorized, options])

  const loadSchedules = useCallback(async () => {
    if (!selectedId) { setSchedules([]); setScheduleId(''); return }
    try {
      const items = await operationsApi.serviceSchedules(selectedId, options)
      setSchedules(items)
      setScheduleId((current) => current && items.some((item) => item.scheduleId === current) ? current : items[0]?.scheduleId ?? '')
    } catch (failure) { setError(problem(failure, onUnauthorized)) }
  }, [onUnauthorized, options, selectedId])

  useEffect(() => { void Promise.resolve().then(load) }, [load])
  useEffect(() => { void Promise.resolve().then(loadSchedules) }, [loadSchedules])

  function clearEditor() { setAdding(false); setEditing(null); setError('') }

  async function saveService(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const values = new FormData(event.currentTarget)
    const body = { serviceCode: String(values.get('serviceCode') ?? '').trim(), routeId: String(values.get('routeId') ?? ''), displayName: String(values.get('displayName') ?? '').trim() || null, publicServiceNumber: String(values.get('publicServiceNumber') ?? '').trim() || null }
    setBusy(true); setError('')
    try {
      const saved = editing ? await operationsApi.updateTrainService(editing.serviceId, body, options) : await operationsApi.createTrainService(body, options)
      setAdding(false); setEditing(null); setSelectedId(saved.serviceId)
      setNotice(editing ? 'Train service updated.' : 'Train service added. The backend assigned its identifier.')
      await load()
    } catch (failure) { setError(problem(failure, onUnauthorized)) }
    finally { setBusy(false) }
  }

  async function deleteService(service: ManagedTrainService) {
    if (!window.confirm(`Delete ${service.publicServiceNumber ? `${service.publicServiceNumber} · ` : ''}${service.displayName || service.serviceCode}? Services referenced by schedules, journeys, or operating records cannot be deleted.`)) return
    setBusy(true); setError('')
    try { await operationsApi.deleteTrainService(service.serviceId, options); if (selectedId === service.serviceId) setSelectedId(''); setNotice('Train service deleted.'); await load() }
    catch (failure) { setError(problem(failure, onUnauthorized)) }
    finally { setBusy(false) }
  }

  function startNewSchedule() {
    if (!selectedRoute) return
    setScheduleId('')
    setDepartureTime('06:00')
    setCalls(selectedRoute.stops.map((stop, index) => ({ routeStopId: stop.routeStopId, arrivalOffsetMinutes: index === 0 ? null : index * 30, departureOffsetMinutes: index === 0 ? 0 : index === selectedRoute.stops.length - 1 ? null : index * 30 + 2 })))
    setEditingSchedule(true); setError('')
  }

  function startEditSchedule(schedule: ManagedSchedule) {
    setScheduleId(schedule.scheduleId)
    setDepartureTime(timeValue(schedule.firstDepartureLocalTime))
    setCalls(schedule.callingPoints.map(({ routeStopId, arrivalOffsetMinutes, departureOffsetMinutes }) => ({ routeStopId, arrivalOffsetMinutes, departureOffsetMinutes })))
    setEditingSchedule(true); setError('')
  }

  function toggleStop(routeStopId: string, enabled: boolean) {
    if (!enabled) { setCalls((current) => current.filter((call) => call.routeStopId !== routeStopId)); return }
    if (!selectedRoute) return
    const stop = selectedRoute.stops.find((item) => item.routeStopId === routeStopId)
    if (!stop) return
    setCalls((current) => [...current, { routeStopId, arrivalOffsetMinutes: stop.stopOrder * 30, departureOffsetMinutes: stop.stopOrder * 30 + 2 }]
      .sort((left, right) => selectedRoute.stops.findIndex((item) => item.routeStopId === left.routeStopId) - selectedRoute.stops.findIndex((item) => item.routeStopId === right.routeStopId)))
  }

  function updateCall(routeStopId: string, key: 'arrivalOffsetMinutes' | 'departureOffsetMinutes', value: string) {
    setCalls((current) => current.map((call) => call.routeStopId === routeStopId ? { ...call, [key]: value === '' ? null : Number(value) } : call))
  }

  async function saveSchedule(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!selectedService || !departureTime) return
    setBusy(true); setError('')
    try {
      const body = { firstDepartureLocalTime: `${departureTime}:00`, callingPoints: calls }
      if (scheduleId) await operationsApi.updateServiceSchedule(selectedService.serviceId, scheduleId, body, options)
      else await operationsApi.createServiceSchedule(selectedService.serviceId, body, options)
      setEditingSchedule(false); setNotice(scheduleId ? 'Draft timetable updated.' : 'Timetable revision created.')
      await loadSchedules()
    } catch (failure) { setError(problem(failure, onUnauthorized)) }
    finally { setBusy(false) }
  }

  async function publishSchedule(schedule: ManagedSchedule) {
    if (!selectedService || !window.confirm(`Publish timetable revision ${schedule.revision}? The current published revision will remain in history.`)) return
    setBusy(true); setError('')
    try { await operationsApi.publishServiceSchedule(selectedService.serviceId, schedule.scheduleId, options); setNotice(`Timetable revision ${schedule.revision} published.`); await loadSchedules() }
    catch (failure) { setError(problem(failure, onUnauthorized)) }
    finally { setBusy(false) }
  }

  async function deleteSchedule(schedule: ManagedSchedule) {
    if (!selectedService || !window.confirm(`Delete draft timetable revision ${schedule.revision}?`)) return
    setBusy(true); setError('')
    try { await operationsApi.deleteServiceSchedule(selectedService.serviceId, schedule.scheduleId, options); setNotice('Draft timetable deleted.'); await loadSchedules() }
    catch (failure) { setError(problem(failure, onUnauthorized)) }
    finally { setBusy(false) }
  }

  async function saveOperationDates() {
    if (!selectedService || !selectedSchedule) return
    setBusy(true); setError('')
    try { await operationsApi.setServiceOperationDates(selectedService.serviceId, selectedSchedule.scheduleId, operationDates, options); setNotice('Operating dates saved.'); await loadSchedules() }
    catch (failure) { setError(problem(failure, onUnauthorized)) }
    finally { setBusy(false) }
  }

  async function saveManifest(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!manifestDate) return
    const values = new FormData(event.currentTarget)
    setBusy(true); setError('')
    try {
      const result = await operationsApi.setServiceDateManifest(manifestDate, {
        sourceRevision: String(values.get('sourceRevision') ?? '').trim(), fixtureVersion: String(values.get('fixtureVersion') ?? '').trim(),
        isComplete: manifestComplete, contentHash: String(values.get('contentHash') ?? '').trim(), scheduleIds: manifestComplete ? manifestScheduleIds : [],
      }, options)
      setManifestSource(result.sourceRevision); setManifestFixture(result.fixtureVersion); setManifestHash(result.contentHash)
      setNotice(`Manifest for ${manifestDate} saved${result.isComplete ? ' as complete' : ' as incomplete'}.`)
      await loadSchedules()
    } catch (failure) { setError(problem(failure, onUnauthorized)) }
    finally { setBusy(false) }
  }

  async function loadManifest(date: string) {
    setManifestDate(date)
    if (!date) { setManifestScheduleIds([]); return }
    try {
      const current = await operationsApi.serviceDateManifest(date, options)
      setManifestSource(current.sourceRevision); setManifestFixture(current.fixtureVersion); setManifestHash(current.contentHash)
      setManifestComplete(current.isComplete); setManifestScheduleIds(current.scheduleIds)
    } catch (failure) {
      if (!(failure instanceof ApiRequestError && failure.status === 404)) setError(problem(failure, onUnauthorized))
      else { setManifestSource(''); setManifestFixture(''); setManifestHash(''); setManifestComplete(true); setManifestScheduleIds([]) }
    }
  }

  function loadScheduleForEdit(schedule: ManagedSchedule) {
    setScheduleId(schedule.scheduleId)
    setOperationDates(schedule.operationDates.map((item) => item.operationDate))
  }

  return <section className="operations__panel train-service-panel" aria-label="Train service management">
    <div className="operations__panel-heading"><div><span className="operations__section-kicker">RAILWAY NETWORK</span><h2>Train Services</h2><p>Manage train services, timetable revisions, calling points, and operating date evidence.</p></div><button className="button button--primary" type="button" onClick={() => { setEditing(null); setAdding(true); setError('') }}>Add Train Service</button></div>
    {error && <div className="operations__feedback operations__feedback--error" role="alert">{error}</div>}
    {notice && <div className="operations__feedback" role="status">{notice}</div>}
    {(adding || editing) && <form className="operations-form train-service-form" onSubmit={(event) => void saveService(event)}>
      <label className="operations-control"><span>Service code</span><input name="serviceCode" required maxLength={50} defaultValue={editing?.serviceCode ?? ''} /></label>
      <label className="operations-control"><span>Route</span><select name="routeId" required defaultValue={editing?.routeId ?? ''}><option value="" disabled>Select a route</option>{routes.map((route) => <option key={route.routeId} value={route.routeId}>{route.routeCode} · {route.name}{route.isPublished ? '' : ' (unpublished)'}</option>)}</select></label>
      <label className="operations-control"><span>Display name</span><input name="displayName" maxLength={200} defaultValue={editing?.displayName ?? ''} /></label>
      <label className="operations-control"><span>Public service number</span><input name="publicServiceNumber" maxLength={50} defaultValue={editing?.publicServiceNumber ?? ''} /></label>
      <div className="operations-station-form__actions"><button className="button button--primary" type="submit" disabled={busy || routes.length === 0}>{busy ? 'Saving…' : editing ? 'Save changes' : 'Add service'}</button><button className="button button--secondary" type="button" disabled={busy} onClick={clearEditor}>Cancel</button></div>
    </form>}
    {loading ? <div className="operations-skeleton-list" role="status" aria-label="Loading train services"><span /><span /><span /></div> : services.length === 0 ? <div className="operations-empty"><b>No train services found</b><span>Add a service to start managing timetables.</span></div> : <div className="operations__table-wrap"><table className="operations-table"><caption className="sr-only">Train services</caption><thead><tr><th>Public number</th><th>Service</th><th>Route</th><th>Status</th><th>Actions</th></tr></thead><tbody>{services.map((service) => <tr key={service.serviceId}><td><b>{service.publicServiceNumber || '—'}</b></td><td><b>{service.displayName || service.serviceCode}</b><small className="operations-table__note">{service.serviceCode} · {service.serviceId}</small></td><td>{service.routeCode} · {service.routeName}</td><td>{service.isPublished ? 'Published' : 'Draft'}</td><td><div className="operations-station-actions"><button className="button button--secondary button--small" type="button" disabled={busy} onClick={() => { setEditing(service); setAdding(false); setError('') }}>Edit</button><button className="button button--secondary button--small" type="button" disabled={busy} onClick={() => void deleteService(service)}>Delete</button><button className="button button--secondary button--small" type="button" onClick={() => { setSelectedId(service.serviceId); setEditingSchedule(false); setError('') }}>Timetable</button></div></td></tr>)}</tbody></table></div>}

    {selectedService && <div className="train-service-detail">
      <div className="operations__panel-heading"><div><span className="operations__section-kicker">TIMETABLE MANAGEMENT</span><h3>{selectedService.publicServiceNumber ? `${selectedService.publicServiceNumber} · ` : ''}{selectedService.displayName || selectedService.serviceCode}</h3><p>{selectedService.routeCode} · {selectedService.routeName} · Calls follow the route’s established stop sequence.</p></div><button className="button button--primary" type="button" disabled={!selectedRoute?.stops.length || busy} onClick={startNewSchedule}>Create timetable revision</button></div>
      {schedules.length === 0 && !editingSchedule ? <div className="operations-empty"><b>No timetable revisions</b><span>Create a draft revision with at least two route calling points.</span></div> : <div className="train-schedule-list">{schedules.map((schedule) => <article key={schedule.scheduleId} className="train-schedule-card"><div className="train-schedule-card__heading"><div><b>Revision {schedule.revision}</b><span className={schedule.isPublished && !schedule.supersededAt ? 'train-status train-status--published' : schedule.isPublished ? 'train-status' : 'train-status train-status--draft'}>{schedule.supersededAt ? 'Historical' : schedule.isPublished ? 'Published' : 'Draft'}</span><small>First departure {timeValue(schedule.firstDepartureLocalTime)} · {schedule.scheduleId}</small></div><div className="operations-station-actions"><button className="button button--secondary button--small" type="button" onClick={() => loadScheduleForEdit(schedule)}>Operating dates</button>{!schedule.isPublished && <><button className="button button--secondary button--small" type="button" disabled={busy} onClick={() => startEditSchedule(schedule)}>Edit</button><button className="button button--secondary button--small" type="button" disabled={busy} onClick={() => void publishSchedule(schedule)}>Publish</button><button className="button button--secondary button--small" type="button" disabled={busy} onClick={() => void deleteSchedule(schedule)}>Delete</button></>}</div></div>
          {schedule.callingPoints.length === 0 ? <p className="operations__muted">No calling points.</p> : <ol className="train-calling-points">{schedule.callingPoints.map((call) => <li key={call.routeStopId}><b>{call.stopOrder}. {call.stationName}</b><span>Arr {call.arrivalOffsetMinutes ?? '—'} min · Dep {call.departureOffsetMinutes ?? '—'} min</span></li>)}</ol>}
          <div className="train-operation-dates"><b>Operating dates and manifest completeness</b>{schedule.operationDates.length ? schedule.operationDates.map((item) => <span key={item.operationDate}>{item.operationDate} · {item.isManifestComplete ? `Complete (${item.manifestSourceRevision})` : 'Incomplete manifest'}</span>) : <span>No operating dates assigned.</span>}</div>
        </article>)}</div>}

      {editingSchedule && selectedRoute && <form className="train-timetable-editor" onSubmit={(event) => void saveSchedule(event)}><div className="train-timetable-editor__heading"><div><h4>{scheduleId ? 'Edit draft timetable' : 'New timetable revision'}</h4><p>Arrival and departure values are minute offsets from the first departure time.</p></div><button type="button" className="button button--secondary button--small" onClick={() => setEditingSchedule(false)}>Close</button></div>
        <label className="operations-control"><span>First departure (Asia/Colombo)</span><input type="time" required value={departureTime} onChange={(event) => setDepartureTime(event.target.value)} /></label>
        <div className="operations__table-wrap"><table className="operations-table"><thead><tr><th>Call order / station</th><th>Include</th><th>Arrival offset (min)</th><th>Departure offset (min)</th></tr></thead><tbody>{selectedRoute.stops.map((stop) => { const call = calls.find((item) => item.routeStopId === stop.routeStopId); return <tr key={stop.routeStopId}><td><b>{stop.stopOrder}. {stop.stationName}</b></td><td><input aria-label={`Include ${stop.stationName}`} type="checkbox" checked={Boolean(call)} onChange={(event) => toggleStop(stop.routeStopId, event.target.checked)} /></td><td><input aria-label={`Arrival offset at ${stop.stationName}`} type="number" min={0} value={call?.arrivalOffsetMinutes ?? ''} disabled={!call} onChange={(event) => updateCall(stop.routeStopId, 'arrivalOffsetMinutes', event.target.value)} /></td><td><input aria-label={`Departure offset at ${stop.stationName}`} type="number" min={0} value={call?.departureOffsetMinutes ?? ''} disabled={!call} onChange={(event) => updateCall(stop.routeStopId, 'departureOffsetMinutes', event.target.value)} /></td></tr>})}</tbody></table></div>
        <p className="operations__muted">Calling points use the route’s sequence. Published revisions cannot be edited; create a replacement revision to update a published timetable.</p><div className="operations-station-form__actions"><button className="button button--primary" type="submit" disabled={busy || calls.length < 2}>{busy ? 'Saving…' : 'Save draft revision'}</button><button className="button button--secondary" type="button" onClick={() => setEditingSchedule(false)}>Cancel</button></div>
      </form>}

      {selectedSchedule && <div className="train-date-tools"><section className="train-timetable-editor"><div className="train-timetable-editor__heading"><div><h4>Operating dates · revision {selectedSchedule.revision}</h4><p>Assign only dates with a complete service-date manifest.</p></div></div><div className="train-date-add"><label className="operations-control"><span>Add operating date</span><input type="date" value={newOperationDate} onChange={(event) => setNewOperationDate(event.target.value)} /></label><button className="button button--secondary" type="button" disabled={!newOperationDate || operationDates.includes(newOperationDate)} onClick={() => { setOperationDates((current) => [...current, newOperationDate].sort()); setNewOperationDate('') }}>Add date</button></div><ul className="train-date-list">{operationDates.map((date) => <li key={date}><span>{date}</span><button type="button" className="button button--secondary button--small" onClick={() => setOperationDates((current) => current.filter((item) => item !== date))}>Remove</button></li>)}</ul><button className="button button--primary" type="button" disabled={busy} onClick={() => void saveOperationDates()}>Save operating dates</button></section>
        <form className="train-timetable-editor" onSubmit={(event) => void saveManifest(event)}><div className="train-timetable-editor__heading"><div><h4>Date manifest</h4><p>Manifest completeness is date-wide and schedule assignments include every operating service.</p></div></div><label className="operations-control"><span>Operation date</span><input type="date" required value={manifestDate} onChange={(event) => void loadManifest(event.target.value)} /></label><label className="operations-control"><span>Source revision</span><input name="sourceRevision" required maxLength={200} value={manifestSource} onChange={(event) => setManifestSource(event.target.value)} /></label><label className="operations-control"><span>Fixture version</span><input name="fixtureVersion" required maxLength={100} value={manifestFixture} onChange={(event) => setManifestFixture(event.target.value)} /></label><label className="operations-control"><span>Content hash</span><input name="contentHash" required maxLength={200} value={manifestHash} onChange={(event) => setManifestHash(event.target.value)} /></label><label className="train-check"><input type="checkbox" checked={manifestComplete} onChange={(event) => { setManifestComplete(event.target.checked); if (!event.target.checked) setManifestScheduleIds([]) }} /> Mark date manifest complete</label><div className="train-manifest-schedules"><b>Operating published revisions</b>{publishedSchedules.length ? publishedSchedules.map(({ serviceId, label, schedule }) => <label key={schedule.scheduleId}><input type="checkbox" disabled={!manifestComplete} checked={manifestScheduleIds.includes(schedule.scheduleId)} onChange={(event) => setManifestScheduleIds((current) => event.target.checked ? [...current, schedule.scheduleId] : current.filter((id) => id !== schedule.scheduleId))} />{label}{serviceId === selectedId ? ' (selected service)' : ''}</label>) : <span>No current published service revisions.</span>}</div><button className="button button--primary" type="submit" disabled={busy || !manifestDate}>{busy ? 'Saving…' : 'Save date manifest'}</button></form></div>}
    </div>}
  </section>
}
