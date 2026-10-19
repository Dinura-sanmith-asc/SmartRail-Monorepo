import type { StationDeparture } from '../model/display-model'
import { OperationalChange } from './operational-change'

interface DepartureRowProps {
  departure: StationDeparture
}

export function DepartureRow({ departure }: DepartureRowProps) {
  return (
    <li className="departure-board__item">
      <article aria-label={`${departure.serviceLabel} departure`} className="departure-row">
        <div className="departure-row__layout">
          <div className="departure-row__identity">
            <h3>{departure.serviceLabel}</h3>
            <p>Service date: {departure.serviceDate}</p>
          </div>
          <p className="departure-row__scheduled">
            <span>Scheduled departure</span>
            <time>{departure.scheduledTime.displayValue}</time>
          </p>
          <OperationalChange operational={departure.operational} />
        </div>
      </article>
    </li>
  )
}
