import type { StationDeparture } from '../model/display-model'
import { OperationalChange } from './operational-change'
import { useDisplayLanguage } from '../../../app/language'

interface DepartureRowProps {
  departure: StationDeparture
}

export function DepartureRow({ departure }: DepartureRowProps) {
  const { t } = useDisplayLanguage()
  const callType = departure.callType ?? 'departure'
  return (
    <li className="departure-board__item">
      <article
        aria-label={`${departure.serviceLabel} ${callType}`}
        className="departure-row"
        data-departure-id={departure.departureId}
      >
        <div className="departure-row__layout">
          <div className="departure-row__identity">
        <h3>{departure.serviceLabel}{departure.identityAmbiguous ? ' — ambiguous identity' : ''}</h3>
            <p>{t('Service date:')} {departure.serviceDate}</p>
            {departure.routeLabel ? <p>{departure.routeLabel}</p> : null}
          </div>
          <p className="departure-row__scheduled">
            <span>{t(callType === 'arrival' ? 'Scheduled arrival' : 'Scheduled departure')}</span>
            <time>{departure.scheduledTime.displayValue}</time>
          </p>
          <OperationalChange operational={departure.operational} callType={callType} />
        </div>
      </article>
    </li>
  )
}
