import type { OperationalInformation, PublishedValue } from '../model/display-model'

interface OperationalChangeProps {
  operational: OperationalInformation
}

function renderValue(label: string, value: PublishedValue<string>) {
  if (value.kind === 'known') return <p>{label}: {value.value}</p>
  return <p>{label} {value.kind}</p>
}

export function OperationalChange({ operational }: OperationalChangeProps) {
  return (
    <section aria-label="Operational information" className="mt-3 space-y-1 text-base text-slate-200">
      {renderValue('Status', operational.status)}
      {renderValue('Platform', operational.platform)}
      {renderValue('Delay', operational.delay)}
      {operational.cancellation.kind === 'known' ? (
        operational.cancellation.value ? (
          <p className="font-bold text-rose-200" role="status">! Cancelled</p>
        ) : null
      ) : (
        <p>Cancellation status {operational.cancellation.kind}</p>
      )}
      {operational.updateTime?.meaning === 'fixture-defined' ? (
        <p>Fixture update time: {operational.updateTime.displayValue}</p>
      ) : operational.updateTime?.meaning === 'authoritative-verified' ? (
        <p>Updated: {operational.updateTime.displayValue}</p>
      ) : null}
    </section>
  )
}
