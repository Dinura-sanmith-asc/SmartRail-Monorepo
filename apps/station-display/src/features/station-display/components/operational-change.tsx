import type { OperationalInformation, PublishedValue } from '../model/display-model'
import { useDisplayLanguage } from '../../../app/language'

interface OperationalChangeProps {
  operational: OperationalInformation
  callType?: 'arrival' | 'departure'
}

function renderValue(label: string, value: PublishedValue<string>, kind = '', t: (text: string) => string) {
  const text = value.kind === 'known' ? value.value : value.kind === 'unknown' ? t('Unknown') : t('Unavailable')
  return (
    <div className={`operational-change__entry ${kind}`}>
      <dt>{label}</dt>
      <dd>{text}</dd>
    </div>
  )
}

export function OperationalChange({ operational, callType = 'departure' }: OperationalChangeProps) {
  const { t } = useDisplayLanguage()
  return (
    <section aria-label={t('Operational information')} className="operational-change">
      <dl className="operational-change__grid">
        {renderValue(t('Status'), operational.status, operational.status.kind === 'known' ? 'operational-change__entry--status' : '', t)}
        {renderValue(t('Platform'), operational.platform, '', t)}
        {callType === 'arrival' && operational.operationalArrivalTime
          ? renderValue(t('Operational arrival'), operational.operationalArrivalTime, '', t)
          : null}
        {callType === 'departure' && operational.operationalDepartureTime
          ? renderValue(t('Operational departure'), operational.operationalDepartureTime, '', t)
          : null}
        {renderValue(t('Delay'), operational.delay, operational.delay.kind === 'known' ? 'operational-change__entry--delay' : '', t)}
        <div className={`operational-change__entry${operational.cancellation.kind === 'known' && operational.cancellation.value ? ' operational-change__entry--cancelled' : ''}`}>
          <dt>{t('Cancellation')}</dt>
          <dd>
            {operational.cancellation.kind === 'known'
              ? operational.cancellation.value
                ? <span role="status" aria-label={`${t('Cancellation status:')} ${t('Cancelled')}`}>{t('Cancelled')}</span>
                : t('No cancellation reported')
              : operational.cancellation.kind === 'unknown' ? t('Unknown') : t('Unavailable')}
          </dd>
        </div>
        {operational.updateTime?.meaning === 'fixture-defined' ? (
          <div className="operational-change__entry operational-change__entry--update-time"><dt>{t('Fixture update time')}</dt><dd>{operational.updateTime.displayValue}</dd></div>
        ) : operational.updateTime?.meaning === 'authoritative-verified' ? (
          <div className="operational-change__entry operational-change__entry--update-time"><dt>{t('Updated')}</dt><dd>{operational.updateTime.displayValue}</dd></div>
        ) : null}
      </dl>
    </section>
  )
}
