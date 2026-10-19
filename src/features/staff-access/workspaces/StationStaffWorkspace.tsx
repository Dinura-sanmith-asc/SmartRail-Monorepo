import { NavLink } from 'react-router'
import { useIdentity } from '../identity/identityContext'
import { getWorkspaceAffordances } from '../routes/rolePresentation'
import './StationStaffWorkspace.css'

export function StationStaffWorkspace() {
  const { identity } = useIdentity()
  const affordances = getWorkspaceAffordances(identity?.roles ?? [])

  return (
    <main className="app-shell station-staff-shell" aria-labelledby="station-staff-heading">
      <header className="station-staff-shell__header">
        <p className="app-shell__eyebrow">SmartRail Operations</p>
        <h1 id="station-staff-heading">Station Staff Workspace</h1>
        <p className="station-staff-shell__description">
          Your operational workspace is ready. Station and resource access is checked by the relevant service.
        </p>
      </header>
      <nav className="workspace-navigation" aria-label="Operations navigation">
        {affordances.map(({ role, label, href }) => (
          <NavLink key={role} to={href} end className="workspace-navigation__link">
            {label}
          </NavLink>
        ))}
      </nav>
      <section className="station-staff-shell__content" aria-label="Station Staff workspace">
        <p>Operational services are not shown in this workspace.</p>
      </section>
    </main>
  )
}
