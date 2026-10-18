# Quickstart: Station Display Validation

This guide describes validation scenarios for implementation. It does not claim the feature or future live endpoints are currently implemented.

## Prerequisites

- Node.js 24.x (the inspected local environment is 24.18.0; the installed Vite 8 requires Node 20.19+ or 22.12+).
- npm and repository dependencies installed with the committed lockfile.
- During fixture-only UI work, use an explicitly labeled contract fixture. Do not identify fixture output as live railway data.
- For live acceptance, an environment with the approved Journeys & Status contract and a published test station/departure dataset is required.

## Current repository checks

The existing scripts are:

```powershell
npm run lint
npm run build
```

These validate linting and TypeScript/production compilation. They do not prove acceptance behavior or live integration.

## Planned feature test commands

Implementation tasks must add and verify test scripts before relying on them:

```powershell
npm run test:unit
npm run test:e2e
```

Use Vitest/React Testing Library for state and component behavior and Playwright for browser scenarios. Playwright should run the Vite app with its test web server; viewport contexts must use the two approved landscape profiles.

## Validation scenarios

1. **Configured station identity**: Set the deployment station ID to a published station in the fixture; confirm the board title matches its published name. Repeat with missing, malformed and unpublished IDs; expect configuration/unavailable state and no fallback station.
2. **Station isolation and schedule meaning**: Feed departures for two station IDs through a labeled fixture; confirm only the configured station's dated departures are shown. Confirm a reusable service search result alone never appears as a confirmed station departure.
3. **Operational changes**: Provide source-authoritative platform, delay and cancellation fixture values for a dated departure. Confirm the correct row changes, scheduled values remain clearly planned, text/symbol communicates status and update time is shown only if supplied with verified semantics. Leave some operational values absent and confirm none are inferred.
4. **Automatic update**: In a controlled adapter fixture, make a newer authoritative response available while the board is connected. Confirm the board updates without user action. Do not assert a fabricated interval or latency.
5. **Connectivity and recovery**: After one successful response, interrupt the source; confirm the prior data is visibly cached/possibly outdated and last update appears only when reliable. Restore connectivity; confirm stale indication remains until a successful authoritative revalidation. Start offline with no prior response and confirm unavailable state.
6. **Approved display profiles**: Run the primary board scenario at 1920 × 1080 and 1366 × 768. Confirm station identity, scheduled time, service identity and operational status remain readable and important disruption information is not hidden at either profile.
7. **Accessibility and passive use**: Inspect semantic headings/status announcements and WCAG 2.2 AA criteria. Confirm status is understandable without color. Confirm no station selector, station-management, controller or kiosk actions are exposed.
8. **Live contract acceptance (blocked)**: Repeat the station/departure, operational update, automatic refresh and recovery scenarios against the backend only after its contract, public projection, revision/time semantics and error behavior are approved. Record source contract version and verification evidence; fixtures alone cannot pass this gate.

## Expected result

Fixture scenarios may validate the independent UI and state presentation. The feature is not live-integration complete until the future endpoint contracts are verified and the display passes the source acceptance checks for SR-054, SR-056, SR-058 and SR-059, with the station prototype evidence for SR-061 and applicable cross-cutting requirement evidence.
