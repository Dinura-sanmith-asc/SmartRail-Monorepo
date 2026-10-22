# Research: Journey Status and Updates

**Date**: 2026-10-20
**Scope**: Feature 002 technical decisions for the existing SmartRail Passenger Flutter app. Backend implementation and contract reviewed in the sibling `SmartRail` repository at `specs/003-journeys-status/`.

## Decision: Keep Feature 002 isolated behind its own feature boundary

**Decision**: Add `lib/features/journey_status/` with presentation, application, domain, and data/API boundaries. Keep Feature 001's railway search models and repository authoritative for scheduled service inputs. Feature 002 receives its selected service, origin station, and `TravelDate` as navigation context and uses a separate journey repository for dated departure candidates, snapshots, and update history.

**Rationale**: The existing app already follows this separation in `lib/features/railway_search/`. Journey instance identity and live status are separate concepts from reusable service schedule identity. A separate feature package prevents journey DTOs and states from leaking into search/results/details behavior.

**Alternatives considered**: Extending Feature 001's service models/repository with operational journey state would conflate scheduled and dated entities and enlarge the regression surface. A new Flutter package would add no needed boundary.

## Decision: Reuse the app's composition, networking, and UI foundations

**Decision**: Reuse `flutter_riverpod`, `go_router`, `http`, `intl`, the app's environment configuration, `sharedHttpClientProvider`, `HttpApiClient`, typed `ApiFailure`/Problem Details handling, common async feedback, and `RetrievalMetadata`. Add only journey-specific providers and mappings. Do not add packages or code generation.

**Rationale**: These dependencies are already selected and implemented. `sharedHttpClientProvider` owns and closes the one `http.Client`. Journey API wrappers may reuse it with the configured `AppConfig` base URI; the journey feature must not construct or close a second shared transport. Existing request-generation, auto-dispose, and fake repository tests provide tested patterns.

**Alternatives considered**: Adding a second HTTP stack, DI framework, JSON generator, persistent cache, or notification plugin has no contract-backed need and would add dependencies or duplicate lifecycle rules.

## Decision: Keep API DTOs and domain models strict and separate

**Decision**: Parse only the three approved public GET contracts in `contracts/journey-status-client.md`, using explicit typed DTO parsing in the journey data layer. Map those DTOs to immutable journey domain models and validate required fields, enum values, UUID strings, nullable fields, and discriminated public changes. Malformed successful payloads become protocol failures, not partial confirmed data.

**Rationale**: The backend OpenAPI schemas are closed and use stable explicit fields. Journey IDs, service IDs, update IDs, schedule IDs, and stop IDs have distinct meanings. Parsing at the data boundary protects passenger state from malformed or private fields.

**Alternatives considered**: Sharing Feature 001 DTOs is incorrect because the schemas represent different entities; generated parsing is unnecessary for this fixed scope and would add build tooling.

## Decision: Resolve service-to-journey selection through station departures

**Decision**: From Feature 001 details, query `GET /api/stations/{originStationId}/departures?date={travelDate}`. Keep only rows whose `serviceId` equals the selected scheduled service and whose `stationLocalDepartureDate` equals the selected date. If exactly one matching candidate is marked `none`, it can open directly. Otherwise present the returned candidates as distinct explicit choices; require a tap for every ambiguous candidate. Pass the chosen `journeyInstanceId` to detail/history reads.

**Rationale**: Backend test `JourneySelectionFlowTests.Feature001_search_to_departure_choices_to_selected_journey_keeps_instances_distinct_and_marks_ambiguity` demonstrates that one service/date can have multiple dated journeys, including candidates with identical public descriptors. The API marks ambiguous candidates and deliberately does not choose. This is also the approved Q1 decision.

**Alternatives considered**: Using the scheduled `serviceId` as a journey ID, deriving journey IDs from dates, or selecting the first row would be incorrect and unsupported. An accessible ordinal such as “Option 1 of 2” can distinguish controls without claiming to distinguish backend data.

## Decision: Treat first successful update history as the active-session baseline

**Decision**: Keep an app-session tracker keyed by `journeyInstanceId` and containing successfully observed `updateId` values. On the first successful history read for a journey in the active app session, establish the baseline without alerting for the pre-existing history. On later journey-view entries or manual refreshes, compare new IDs against that baseline and alert only for newly detected supported significant changes. Advance the baseline only after a successful, valid history response. A failed read does not mark anything seen. Reset the tracker when the app session ends.

**Rationale**: Q2 allows alerts only when opening or manual refresh detects a new update, and prohibits polling or background claims. Without an earlier successful read in the session, the app cannot determine that an item is new; treating the first response as baseline avoids presenting the full historical log as a burst of new alerts. App-session scope prevents repeated alerts when navigating away and back. The API's immutable update ID and revision provide stable comparison keys.

**Alternatives considered**: Alerting on every item at initial load would imply old changes are newly detected; storing IDs across app restarts would introduce persistence outside the approved session scope. Polling or push is not contracted.

## Decision: Refresh snapshot and history independently

**Decision**: Model the journey snapshot and update history as independent async sections. Initial view entry reads both; manual refresh requests both; per-section retry re-requests only that section. Preserve successful data when its sibling read fails. If their revisions differ, do not merge values into one confirmed revision; surface incomplete/not-refreshed context and allow a refresh.

**Rationale**: The reads are independent public endpoints and the specification requires partial success behavior. A publication can happen between requests, so the client must not manufacture cross-response consistency.

**Alternatives considered**: All-or-nothing loading discards useful data when one read fails; merging different revisions implies a snapshot that never existed.

## Decision: Keep source times literal and client retrieval time separate

**Decision**: Preserve each offset-bearing timestamp's original ISO 8601 text and display its literal represented date/time/offset without converting to the device zone. Keep API publication/update time distinct from the client's successful retrieval time. Treat `operationDate` and `stationLocalDepartureDate` as separate date-only fields.

**Rationale**: The backend returns offset-bearing `date-time` fields, while station-departure selection is explicitly based on Asia/Colombo station-local calendar dates. Existing Feature 001 timestamp design establishes an offset-preserving pattern; the journey view must preserve operational source values too.

**Alternatives considered**: Parsing to a device-local `DateTime` could change the visible date/time and conflate a railway source timestamp with the passenger device zone.

## Decision: Map only contract-backed recovery behaviors

**Decision**: Treat 200 empty arrays as valid no-match/no-updates results; 400 as invalid request input, 404 as missing station/journey resource, and 503 as unavailable information. Show safe access-unavailable feedback without login for unexpected 401/403. For unexpected server failures, timeout, network errors, and malformed 2xx bodies, use safe recoverable feedback and retry only the affected read. Do not retry unchanged malformed IDs/dates or access denial; keep Back available.

**Rationale**: These are the exact documented outcomes and the approved public reads are anonymous. The departure request is composed from previously validated Feature 001 selection data; a 400 therefore indicates invalid request context, not an input the passenger can repair on this screen.

**Alternatives considered**: Treating 200 empty as an outage, prompting sign-in, inventing special 500 semantics, or retrying permanent 400/404/403 responses would misstate the contract.

## Decision: Keep integration test evidence explicit

**Decision**: Use fixture-backed API, model, repository, Riverpod, widget, and navigation tests for deterministic behavior, and keep real-backend integration tests separate. Real integration must use an explicitly configured backend and seeded candidates; unavailable backend/device prerequisites are recorded `BLOCKED` or `NOT RUN`, never passed.

**Rationale**: This follows the existing Feature 001 quickstart and constitution's cross-system acceptance requirement. Unit/widget tests do not substitute for backend evidence.

**Alternatives considered**: Mocked-only acceptance does not verify cross-module identity mapping or actual published fields.

## Technical risks and unresolved decisions

- Identical ambiguous candidates can be separate rows but may have no distinct passenger-readable attributes. Preserve each identity and accessible ordinal; this is an upstream projection/UX limitation documented by the approved backend test and Q1.
- The current backend emits `eta: null`; estimate UI should be modeled/tested for the contract shape but must show unavailable for current data.
- There is no push, subscription, or polling API. SR-025 uses the approved in-app open/manual-refresh detection only; delivery latency is not guaranteed.
- Backend HTTP statuses documented by OpenAPI are 400/404/503 for public reads. Unexpected access-denied or server responses get generic safe handling without becoming contract assumptions.
- Before implementation, recheck the approved backend contract at `SmartRail/specs/003-journeys-status/contracts/journeys-status.openapi.yaml`.
