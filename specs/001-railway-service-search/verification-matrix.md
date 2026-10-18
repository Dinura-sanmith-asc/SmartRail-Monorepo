# Feature 001 Verification Matrix

This is the evidence record for Feature 001. All entries begin as `NOT RUN`; update a
status only when the stated check has actually been executed and attach a test result,
command output, or review record.

## Status Definitions

| Status | Meaning | Evidence rule |
|---|---|---|
| `PASS` | The check was executed and every stated assertion passed. | Record the command or review method and link/name its result. Integration requires the actual configured backend and seeded published data. |
| `FAIL` | The check was executed and at least one stated assertion failed. | Record the failing assertion and relevant output. Do not use `FAIL` for an unexecuted check. |
| `BLOCKED` | The check could not be executed because a required external prerequisite is unavailable or an approval gate is unresolved. | Name the missing prerequisite and owner/source if known. It is not a pass and must not count as acceptance evidence. |
| `NOT RUN` | The check has not been attempted, or is intentionally supplementary and was not run. | State the reason when known. It is not a pass and must not count as acceptance evidence. |

Missing backend URL, seeded data, emulator, or physical-device connectivity must be recorded
as `BLOCKED` or `NOT RUN` as appropriate. Reporting those statuses, maintaining requirement
traceability, running local analysis, and running automated tests do not depend on those
external prerequisites. The Android emulator with a real seeded backend is the primary
integration acceptance path. Physical-device smoke is supplementary unless an approved
requirement says otherwise. Never substitute mocks for real-backend evidence or mark an
unexecuted integration run `PASS`.

## Success-Criteria Evidence

| Criterion | Verification tasks | Expected evidence | Status | Evidence / reason |
|---|---|---|---|---|
| SC-001 | T033, T054 | Result/card assertions against supplied search values; emulator integration comparison with backend response. | NOT RUN | |
| SC-002 | T022, T026 | Fake/spy repository search-counter assertions show exactly zero `repository.search` calls separately for missing origin, missing destination, missing travel date, and identical origin/destination; validation is actionable. | NOT RUN | |
| SC-003 | T033, T042, T047 | HTTP 200 with an empty `services` collection shows no results and is not confused with validation, backend, or network failures; error mapping/recovery tests cover 400, 403, 404, 500/default, 503, and no network. | NOT RUN | |
| SC-004 | T033, T040, T054 | Result and detail tests verify backend stop order, supplied local date/time, next-day cue, and planned labels. | NOT RUN | |
| SC-005 | T041, T054 | Navigation tests and emulator flow show exact criteria/results restored after Back. | NOT RUN | |
| SC-006 | T049, T050 | Accessibility semantics, focus, announcements, text scaling, and control operation review. | NOT RUN | |
| SC-007 | T040, T047 | Both partial-failure directions preserve the successful section and targeted retry. | NOT RUN | |

## Requirement and Integration Evidence

T061 adds rows or explicit references for every FR-001–FR-022 and applicable SR requirement,
preserving SR-103/SR-104 as Should Have. Each requirement's status must follow the definitions
above. Record emulator integration separately from fixture/mock evidence. Record supplementary
physical-device smoke separately and do not make it a primary acceptance gate.
