import { NavLink } from 'react-router'
import { useIdentity } from '../identity/identityContext'
import { getWorkspaceAffordances } from '../routes/rolePresentation'

export function ControllerWorkspace() {
  const { identity } = useIdentity()
  const affordances = getWorkspaceAffordances(identity?.roles ?? [])

  return (
    <main className="app-shell" aria-labelledby="controller-heading">
      <header>
        <p className="app-shell__eyebrow">SmartRail Operations</p>
        <h1 id="controller-heading">Controller Workspace</h1>
      </header>
      <nav className="workspace-navigation" aria-label="Operations navigation">
        {affordances.map(({ role, label, href }) => (
          <NavLink key={role} to={href} end className="workspace-navigation__link">
            {label}
          </NavLink>
        ))}
      </nav>
      <section aria-label="Controller workspace">
        <p>Authorized Controller access is active.</p>
      </section>
    </main>
  )
}
