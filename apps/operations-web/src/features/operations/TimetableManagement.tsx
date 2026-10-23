import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { ApiRequestError } from '../../shared/api/fetchJson'
import { apiErrorMessage, operationsApi, type ManagedRoute, type ManagedSchedule, type ManagedTrainService, type OperationsApiOptions, type ScheduleDraft } from './operationsApi'

type Props = { options: OperationsApiOptions; onUnauthorized: () => void }
type Call = ScheduleDraft['callingPoints'][number]
const weekdays = [{ label: 'Mon', bit: 1 }, { label: 'Tue', bit: 2 }, { label: 'Wed', bit: 3 }, { label: 'Thu', bit: 4 }, { label: 'Fri', bit: 5 }, { label: 'Sat', bit: 6 }, { label: 'Sun', bit: 0 }]
const localDate = () => new Date().toLocaleDateString('en-CA', { timeZone: 'Asia/Colombo' })
const minutes = (time: string) => { const [hour, minute] = time.split(':').map(Number); return hour * 60 + minute }
const timeAt = (first: string, offset: number | null) => offset === null ? '' : `${String(Math.floor(((minutes(first) + offset) % 1440) / 60)).padStart(2, '0')}:${String((minutes(first) + offset) % 60).padStart(2, '0')}`
const dayAt = (first: string, offset: number | null) => offset === null ? 0 : Math.floor((minutes(first) + offset) / 1440)

export function TimetableManagement({ options, onUnauthorized }: Props) {
  const [services, setServices] = useState<ManagedTrainService[]>([])
  const [routes, setRoutes] = useState<ManagedRoute[]>([])
  const [serviceId, setServiceId] = useState('')
  const [schedules, setSchedules] = useState<ManagedSchedule[]>([])
  const [draftId, setDraftId] = useState<string | null>(null)
  const [draft, setDraft] = useState<ScheduleDraft | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const service = services.find((item) => item.serviceId === serviceId)
  const route = routes.find((item) => item.routeId === service?.routeId)

  function failure(reason: unknown) {
    if (reason instanceof ApiRequestError && reason.status === 401) onUnauthorized()
    setError(apiErrorMessage(reason))
  }

  const loadServices = useCallback(async () => {
    try {
      const [serviceRows, routeRows] = await Promise.all([operationsApi.managedServices(options), operationsApi.managedRoutes(options)])
      setServices(serviceRows); setRoutes(routeRows)
    } catch (reason) { failure(reason) }
  }, [options, onUnauthorized])
  const loadSchedules = useCallback(async () => {
    if (!serviceId) { setSchedules([]); return }
    try { setSchedules(await operationsApi.serviceSchedules(serviceId, options)) } catch (reason) { failure(reason) }
  }, [serviceId, options, onUnauthorized])
  useEffect(() => { void loadServices() }, [loadServices])
  useEffect(() => { void loadSchedules() }, [loadSchedules])

  function begin(source?: ManagedSchedule) {
    if (!service || service.routeStopIds.length < 2) return
    const today = localDate()
    const yearLater = new Date(`${today}T00:00:00Z`)
    yearLater.setUTCDate(yearLater.getUTCDate() + 365)
    const orderedIds = source ? source.callingPoints.map((call) => call.routeStopId) : service.routeStopIds
    const calls = orderedIds.map((routeStopId, index) => {
      const existing = source?.callingPoints.find((item) => item.routeStopId === routeStopId)
      return { routeStopId, arrivalOffsetMinutes: index === 0 ? null : existing?.arrivalOffsetMinutes ?? null,
        departureOffsetMinutes: index === orderedIds.length - 1 ? null : existing?.departureOffsetMinutes ?? (index === 0 ? 0 : null) }
    })
    setDraft({ slotId: source?.slotId, firstDepartureLocalTime: source?.firstDepartureLocalTime.slice(0, 5) ?? '08:00', callingPoints: calls,
      calendar: source?.calendar ? { ...source.calendar, exceptions: [...source.calendar.exceptions] } : { validFrom: today, validThrough: yearLater.toISOString().slice(0, 10), weekdaysMask: 127, exceptions: [] } })
    setDraftId(source && !source.isPublished ? source.scheduleId : null)
    setError('')
    setNotice('')
  }

  function setCall(index: number, field: 'arrivalOffsetMinutes' | 'departureOffsetMinutes', value: number | null) {
    setDraft((current) => current && { ...current, callingPoints: current.callingPoints.map((call, position) => position === index ? { ...call, [field]: value } : call) })
  }

  function setCalendar(change: Partial<ScheduleDraft['calendar']>) {
    setDraft((current) => current && { ...current, calendar: { ...current.calendar, ...change } })
  }

  function setDirection(returnTrip: boolean) {
    if (!service) return
    const ids = returnTrip ? [...service.routeStopIds].reverse() : service.routeStopIds
    setDraft((current) => current && { ...current, callingPoints: ids.map((routeStopId, index) => ({
      routeStopId, arrivalOffsetMinutes: null,
      departureOffsetMinutes: index === 0 ? 0 : null,
    })) })
  }

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!draft || !serviceId) return
    setBusy(true); setError(''); setNotice('')
    try {
      const body = { ...draft, firstDepartureLocalTime: `${draft.firstDepartureLocalTime}:00` }
      if (draftId) await operationsApi.updateServiceSchedule(serviceId, draftId, body, options)
      else await operationsApi.createServiceSchedule(serviceId, body, options)
      setDraft(null); setDraftId(null); setNotice('Draft timetable saved.'); await loadSchedules()
    } catch (reason) { failure(reason) } finally { setBusy(false) }
  }

  async function publish(scheduleId: string) {
    setBusy(true); setError(''); setNotice('')
    try { await operationsApi.publishServiceSchedule(serviceId, scheduleId, options); setNotice('Timetable published.'); await loadSchedules() }
    catch (reason) { failure(reason) } finally { setBusy(false) }
  }

  async function remove(scheduleId: string) {
    if (!window.confirm('Delete this schedule revision? This cannot be undone.')) return
    setBusy(true); setError('')
    try { await operationsApi.deleteServiceSchedule(serviceId, scheduleId, options); setNotice('Schedule revision deleted.'); if (draftId === scheduleId) { setDraft(null); setDraftId(null) } await loadSchedules() }
    catch (reason) { failure(reason) } finally { setBusy(false) }
  }

  function timeField(call: Call, index: number, field: 'arrivalOffsetMinutes' | 'departureOffsetMinutes') {
    if (!draft) return null
    const offset = call[field]
    return <span className="timetable-time"><input type="time" required value={timeAt(draft.firstDepartureLocalTime, offset)} onChange={(event) => { const value = event.target.value; const day = offset === null && minutes(value) < minutes(draft.firstDepartureLocalTime) ? 1 : dayAt(draft.firstDepartureLocalTime, offset); setCall(index, field, value ? day * 1440 + minutes(value) - minutes(draft.firstDepartureLocalTime) : null) }} /><label>Day <input type="number" min="0" max="7" value={dayAt(draft.firstDepartureLocalTime, offset)} onChange={(event) => offset !== null && setCall(index, field, Number(event.target.value) * 1440 + minutes(timeAt(draft.firstDepartureLocalTime, offset)) - minutes(draft.firstDepartureLocalTime))} /></label></span>
  }

  return <section className="operations__panel timetable-panel" aria-label="Timetables and schedules">
    <div className="operations__panel-heading"><div><span className="operations__section-kicker">RAILWAY NETWORK</span><h2>Timetables &amp; Schedules</h2><p>Set calling times and operating days for each train service.</p></div></div>
    {error && <div className="operations__feedback operations__feedback--error" role="alert">{error}</div>}
    {notice && <div className="operations__feedback" role="status">{notice}</div>}
    <label className="operations-control"><span>Train service</span><select value={serviceId} onChange={(event) => { setServiceId(event.target.value); setDraft(null); setDraftId(null); setNotice('') }}><option value="">Select a service</option>{services.map((item) => <option key={item.serviceId} value={item.serviceId}>{item.publicServiceNumber || item.serviceCode} · {item.displayName || item.routeName}</option>)}</select></label>
    {service && <><div className="operations-station-form__actions timetable-actions"><button className="button button--primary" type="button" disabled={busy || service.routeStopIds.length < 2} onClick={() => begin()}>Add schedule</button></div>
      {schedules.map((item) => <div className="timetable-revision" key={item.scheduleId}><div><b>Schedule {Math.min(...schedules.filter((row) => row.slotId === item.slotId).map((row) => row.revision))} · revision {item.revision}</b> · {item.isPublished ? item.supersededAt ? 'Superseded' : 'Published' : 'Draft'} · {item.callingPoints[0]?.stationName} → {item.callingPoints.at(-1)?.stationName} · Departs {item.firstDepartureLocalTime.slice(0, 5)} · {item.calendar ? `${item.calendar.validFrom} to ${item.calendar.validThrough}` : 'No weekly calendar'}</div><div className="operations-station-actions"><button className="button button--secondary button--small" type="button" onClick={() => begin(item)}>{item.isPublished ? 'Revise schedule' : 'Edit draft'}</button>{!item.isPublished && <button className="button button--primary button--small" type="button" disabled={busy || !item.calendar} onClick={() => void publish(item.scheduleId)}>Publish</button>}<button className="button button--secondary button--small" type="button" disabled={busy} onClick={() => void remove(item.scheduleId)}>Delete</button></div></div>)}
      {draft && <form className="operations-form timetable-form" onSubmit={(event) => void save(event)}><h3>{draftId ? 'Edit draft' : draft.slotId ? 'New schedule revision' : 'New schedule'}</h3><label className="operations-control"><span>Direction</span><select value={draft.callingPoints[0]?.routeStopId === service.routeStopIds[0] ? 'outbound' : 'return'} onChange={(event) => setDirection(event.target.value === 'return')}><option value="outbound">{route?.stops.find((stop) => stop.routeStopId === service.routeStopIds[0])?.stationName} → {route?.stops.find((stop) => stop.routeStopId === service.routeStopIds.at(-1))?.stationName}</option><option value="return">{route?.stops.find((stop) => stop.routeStopId === service.routeStopIds.at(-1))?.stationName} → {route?.stops.find((stop) => stop.routeStopId === service.routeStopIds[0])?.stationName}</option></select></label><label className="operations-control"><span>First departure · Asia/Colombo</span><input type="time" required value={draft.firstDepartureLocalTime} onChange={(event) => setDraft((current) => current && { ...current, firstDepartureLocalTime: event.target.value })} /></label><div className="operations__table-wrap"><table className="operations-table"><thead><tr><th>Station</th><th>Arrival</th><th>Departure</th></tr></thead><tbody>{draft.callingPoints.map((call, index) => <tr key={call.routeStopId}><td>{route?.stops.find((stop) => stop.routeStopId === call.routeStopId)?.stationName ?? 'Unknown station'}</td><td>{index === 0 ? 'Start' : timeField(call, index, 'arrivalOffsetMinutes')}</td><td>{index === draft.callingPoints.length - 1 ? 'End' : index === 0 ? draft.firstDepartureLocalTime : timeField(call, index, 'departureOffsetMinutes')}</td></tr>)}</tbody></table></div>
        <div className="timetable-calendar"><label className="operations-control"><span>Valid from</span><input type="date" required value={draft.calendar.validFrom} onChange={(event) => setCalendar({ validFrom: event.target.value })} /></label><label className="operations-control"><span>Valid through</span><input type="date" required value={draft.calendar.validThrough} onChange={(event) => setCalendar({ validThrough: event.target.value })} /></label><fieldset><legend>Operating weekdays</legend>{weekdays.map((day) => <label key={day.bit}><input type="checkbox" checked={(draft.calendar.weekdaysMask & (1 << day.bit)) !== 0} onChange={(event) => setCalendar({ weekdaysMask: event.target.checked ? draft.calendar.weekdaysMask | (1 << day.bit) : draft.calendar.weekdaysMask & ~(1 << day.bit) })} />{day.label}</label>)}</fieldset><fieldset><legend>Date exceptions</legend>{draft.calendar.exceptions.map((item, index) => <div className="timetable-exception" key={index}><input type="date" value={item.date} onChange={(event) => setCalendar({ exceptions: draft.calendar.exceptions.map((exception, position) => position === index ? { ...exception, date: event.target.value } : exception) })} /><select value={item.runs ? 'runs' : 'cancelled'} onChange={(event) => setCalendar({ exceptions: draft.calendar.exceptions.map((exception, position) => position === index ? { ...exception, runs: event.target.value === 'runs' } : exception) })}><option value="runs">Runs</option><option value="cancelled">Does not run</option></select><button className="button button--secondary button--small" type="button" onClick={() => setCalendar({ exceptions: draft.calendar.exceptions.filter((_, position) => position !== index) })}>Remove</button></div>)}<button className="button button--secondary button--small" type="button" onClick={() => setCalendar({ exceptions: [...draft.calendar.exceptions, { date: draft.calendar.validFrom, runs: false }] })}>Add exception</button></fieldset></div><div className="operations-station-form__actions"><button className="button button--primary" disabled={busy || !draft.calendar.weekdaysMask}>{busy ? 'Saving…' : 'Save draft'}</button><button className="button button--secondary" type="button" onClick={() => setDraft(null)}>Cancel</button></div></form>}
    </>}
  </section>
}
