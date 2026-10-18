import type { StationDeparture } from '../model/display-model'
import { OperationalChange } from './operational-change'

interface DepartureRowProps {
  departure: StationDeparture
}

export function DepartureRow({ departure }: DepartureRowProps) {
  return (
    <li className="rounded-lg border border-slate-700 bg-slate-900 p-5 text-white">
      <article aria-label={`${departure.serviceLabel} departure`}>
        <div className="flex flex-wrap items-baseline justify-between gap-3">
          <h3 className="text-xl font-semibold">{departure.serviceLabel}</h3>
          <p className="text-sm text-slate-300">Service date: {departure.serviceDate}</p>
        </div>
        <p className="mt-3 text-2xl font-bold">
          <span className="mr-2 text-base font-medium text-slate-300">Scheduled</span>
          <time>{departure.scheduledTime.displayValue}</time>
        </p>
        <OperationalChange operational={departure.operational} />
      </article>
    </li>
  )
}
