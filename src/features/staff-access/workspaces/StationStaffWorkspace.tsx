import { WorkspaceNavigation } from './WorkspaceNavigation'
import './StationStaffWorkspace.css'

export function StationStaffWorkspace() {
  return (
    <main className="app-shell station-staff-shell" aria-labelledby="station-staff-heading">
      <header className="station-staff-shell__header">
        <p className="app-shell__eyebrow">SmartRail Operations</p>
        <h1 id="station-staff-heading">Station Staff Workspace</h1>
        <p className="station-staff-shell__description">
          Your operational workspace is ready. Station and resource access is checked by the relevant service.
        </p>
      </header>
      <WorkspaceNavigation />
      <section className="station-staff-shell__content" aria-label="Station Staff workspace">
        <p>Operational services are not shown in this workspace.</p>
      </section>
    </main>
  )
}
