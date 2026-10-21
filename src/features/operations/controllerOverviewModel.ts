import type { OperationsOverviewJourney } from './operationsApi'

export type JourneyFilter = 'all' | 'delayed' | OperationsOverviewJourney['status']
export type SortKey = 'service' | 'route' | 'departure' | 'arrival' | 'status' | 'delay' | 'station' | 'updated'
export type SortDirection = 'ascending' | 'descending'

export function colomboToday(now = new Date()): string {
  const parts = new Intl.DateTimeFormat('en', { timeZone: 'Asia/Colombo', year: 'numeric', month: '2-digit', day: '2-digit' }).formatToParts(now)
  const values = Object.fromEntries(parts.map(({ type, value }) => [type, value]))
  return `${values.year}-${values.month}-${values.day}`
}

function delayed(journey: OperationsOverviewJourney) {
  return journey.status === 'Delayed' || (journey.recordedDelayMinutes !== null && journey.recordedDelayMinutes > 0)
}

function sortValue(journey: OperationsOverviewJourney, key: SortKey): string | number | null {
  switch (key) {
    case 'service': return (journey.serviceDisplayName ?? journey.publicServiceNumber ?? journey.serviceCode).toLocaleLowerCase()
    case 'route': return `${journey.origin} ${journey.destination} ${journey.routeName}`.toLocaleLowerCase()
    case 'departure': return journey.scheduledDeparture ? Date.parse(journey.scheduledDeparture) : null
    case 'arrival': return journey.scheduledArrival ? Date.parse(journey.scheduledArrival) : null
    case 'status': return journey.status.toLocaleLowerCase()
    case 'delay': return journey.recordedDelayMinutes
    case 'station': return journey.lastReportedStation?.toLocaleLowerCase() ?? null
    case 'updated': return Date.parse(journey.lastUpdateAt)
  }
}

export function filterSortOverviewJourneys(
  journeys: OperationsOverviewJourney[], query: string, filter: JourneyFilter, sortKey: SortKey, direction: SortDirection,
): OperationsOverviewJourney[] {
  const needle = query.trim().toLocaleLowerCase()
  return journeys.filter((journey) => {
    const searchable = [journey.serviceDisplayName, journey.publicServiceNumber, journey.serviceCode, journey.routeCode,
      journey.routeName, journey.origin, journey.destination].filter(Boolean).join(' ').toLocaleLowerCase()
    const matchesQuery = !needle || searchable.includes(needle)
    const matchesFilter = filter === 'all' || (filter === 'delayed' ? delayed(journey) : journey.status === filter)
    return matchesQuery && matchesFilter
  }).sort((left, right) => {
    const a = sortValue(left, sortKey)
    const b = sortValue(right, sortKey)
    if (a === null && b === null) return 0
    if (a === null) return 1
    if (b === null) return -1
    const compared = typeof a === 'number' && typeof b === 'number' ? a - b : String(a).localeCompare(String(b), undefined, { numeric: true })
    return direction === 'ascending' ? compared : -compared
  })
}
