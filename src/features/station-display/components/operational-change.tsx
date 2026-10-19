import type { OperationalInformation, PublishedValue } from '../model/display-model'

interface OperationalChangeProps {
  operational: OperationalInformation
}

function renderValue(label: string, value: PublishedValue<string>, kind = '') {
  const text = value.kind === 'known' ? value.value : value.kind === 'unknown' ? 'Unknown' : 'Unavailable'
  return (
    <div className={`operational-change__entry ${kind}`}>
      <dt>{label}</dt>
      <dd>{text}</dd>
    </div>
  )
}

export function OperationalChange({ operational }: OperationalChangeProps) {
  return (
    <section aria-label="Operational information" className="operational-change">
      <dl className="operational-change__grid">
        {renderValue('Status', operational.status, operational.status.kind === 'known' ? 'operational-change__entry--status' : '')}
        {renderValue('Platform', operational.platform)}
        {renderValue('Delay', operational.delay, operational.delay.kind === 'known' ? 'operational-change__entry--delay' : '')}
        <div className={`operational-change__entry${operational.cancellation.kind === 'known' && operational.cancellation.value ? ' operational-change__entry--cancelled' : ''}`}>
          <dt>Cancellation</dt>
          <dd>
            {operational.cancellation.kind === 'known'
              ? operational.cancellation.value
                ? <span role="status" aria-label="Cancellation status: Cancelled">Cancelled</span>
                : 'No cancellation reported'
              : operational.cancellation.kind === 'unknown' ? 'Unknown' : 'Unavailable'}
          </dd>
        </div>
        {operational.updateTime?.meaning === 'fixture-defined' ? (
          <div className="operational-change__entry operational-change__entry--update-time"><dt>Fixture update time</dt><dd>{operational.updateTime.displayValue}</dd></div>
        ) : operational.updateTime?.meaning === 'authoritative-verified' ? (
          <div className="operational-change__entry operational-change__entry--update-time"><dt>Updated</dt><dd>{operational.updateTime.displayValue}</dd></div>
        ) : null}
      </dl>
    </section>
  )
}
