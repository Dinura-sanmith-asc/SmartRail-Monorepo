export type DataOrigin = 'fixture' | 'authoritative'

export type PublishedValue<T> =
  | { kind: 'known'; value: T }
  | { kind: 'unknown' }
  | { kind: 'unavailable' }

export interface StationContext {
  stationId: string
  name: string
}

export interface ScheduledDepartureTime {
  /** Source-published scheduled time text; no timezone or date interpretation is inferred. */
  displayValue: string
}

export interface OperationalInformation {
  status: PublishedValue<string>
  platform: PublishedValue<string>
  delay: PublishedValue<string>
  cancellation: PublishedValue<boolean>
}

export interface StationDeparture {
  /** Opaque internal identity for this dated departure. */
  departureId: string
  /** Date label for this fixture/presentation concept; semantics remain source-owned. */
  serviceDate: string
  serviceLabel: string
  scheduledTime: ScheduledDepartureTime
  operational: OperationalInformation
}

export interface StationDepartureSnapshot {
  station: StationContext
  departures: readonly StationDeparture[]
  origin: DataOrigin
}
