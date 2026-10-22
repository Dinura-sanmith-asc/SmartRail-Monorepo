# SmartRail

SmartRail's applications and services live in one repository. Each project keeps its own build and dependency files.

| Directory | Purpose |
| --- | --- |
| `services/backend` | .NET API, Aspire AppHost, tests and backend specifications |
| `apps/operations-web` | Operations React application |
| `apps/station-display` | Station display React application |
| `apps/passenger-mobile` | Passenger Flutter application |
| `apps/driver-mobile` | Driver Flutter application |
| `agents/requirements-workflows` | Requirements and analysis workflows |
| `docs/sdd-handoff` | Architecture and design handoff |
| `packages` | Shared code when a concrete cross-project need emerges |

## Working in the repository

Run each project from its directory. The backend solution is `services/backend/SmartRail.slnx`; the web apps use their own `package.json`; mobile apps use their own `pubspec.yaml`. See each project's README for configuration and run commands.

Changes to API behavior should update its contract and the affected clients together. Keep credentials in local ignored files or deployment secrets. Do not commit files from the old parent folder such as `token.txt`, worktrees, or the handoff ZIP.

See `CONTRIBUTING.md` for changes, new applications, and migration notes.
