import type { StationDeparture } from '../model/display-model'
import { OperationalChange } from './operational-change'
import { useDisplayLanguage } from '../../../app/language'

interface DepartureRowProps {
  departure: StationDeparture
}

export function DepartureRow({ departure }: DepartureRowProps) {
  const { t } = useDisplayLanguage()
  return (
    <li className="departure-board__item">
      <article
        aria-label={`${departure.serviceLabel} departure`}
        className="departure-row"
        data-departure-id={departure.departureId}
      >
        <div className="departure-row__layout">
          <div className="departure-row__identity">
        <h3>{departure.serviceLabel}{departure.identityAmbiguous ? ' — ambiguous identity' : ''}</h3>
            <p>{t('Service date:')} {departure.serviceDate}</p>
          </div>
          <p className="departure-row__scheduled">
            <span>{t('Scheduled departure')}</span>
            <time>{departure.scheduledTime.displayValue}</time>
          </p>
          <OperationalChange operational={departure.operational} />
        </div>
      </article>
    </li>
  )
}
