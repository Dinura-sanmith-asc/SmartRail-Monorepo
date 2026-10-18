# Feature Specification: Railway Search and Service Details

**Feature Branch**: `001-railway-service-search`

**Created**: 2026-10-18

**Status**: Draft

**Input**: User description: "SmartRail Passenger — Railway Search & Service Details"

## Clarifications

### Session 2026-10-18

- Q: When a passenger changes any search criterion while results are displayed, what should happen to the previous results? → A: Clear the visible results as soon as a criterion changes, retain the edited criteria, and show results again only after the passenger submits a new search.
- Q: When a service's destination is scheduled on a calendar day after its origin departure, how should the app display those planned times? → A: Show each call's scheduled date and time as supplied, with a clear "next day" cue when it differs from the selected origin travel date; do not convert it to the passenger's device time zone.
- Q: If the service-detail information loads but the route-stop list fails (or the reverse), what should the passenger see? → A: Show the information that loaded, mark only the unavailable section, and let the passenger retry that section.

## User Scenarios & Testing

### User Story 1 - Search scheduled services (Priority: P1)

As a passenger, I want to search published railway services by origin, destination,
and travel date without signing in, so I can find scheduled options for my trip.

**Why this priority**: Journey search is the approved passenger feature and the primary
entry point to scheduled service information.

**Independent Test**: With published station and service data available, select two
different stations and a date, submit the search anonymously, and verify that the returned
choices match the backend response. Also verify invalid criteria and an empty response.

**Acceptance Scenarios**:

1. **Given** the passenger opens the search screen, **When** station data is loading,
   **Then** the app shows a loading state for station choices without showing search results
   or requiring authentication.
2. **Given** station data has loaded, **When** the initial search screen is ready,
   **Then** origin, destination, and travel date are unset, no validation error is shown,
   and the passenger can choose two different published stations and a date.
3. **Given** origin, destination, and travel date are selected, **When** the passenger
   submits the search, **Then** matching published scheduled service choices are shown with
   the selected travel date and the planned origin and destination calls returned for each;
   each call shows its scheduled local date and time, with a "next day" cue if its date is
   after the selected origin travel date.
4. **Given** the origin and destination are the same or a required criterion is missing,
   **When** the passenger submits the search, **Then** the invalid field or station pair is
   identified in understandable language and no invalid result is presented.
5. **Given** a valid search has no eligible published services, **When** the search
   completes, **Then** an explicit no-services message is shown with the criteria retained
   and an option to change them.
6. **Given** results are displayed, **When** the passenger changes origin, destination, or
   date, **Then** the old results are cleared immediately, the edited criteria remain, and
   no result list is shown until a new search is submitted.
7. **Given** a passenger uses assistive technology or increased text size, **When** they
   complete search and review a result, **Then** controls remain identifiable and operable
   and changing search feedback is available to the accessibility tools.

---

### User Story 2 - Inspect a scheduled service (Priority: P1)

As a passenger, I want to open a result and inspect its planned route and schedule summary,
so I can understand which scheduled service matches my search.

**Why this priority**: Search results have limited value unless a passenger can inspect
the selected service and its planned route information.

**Independent Test**: Open a returned service and verify the detail view identifies the
same service, presents only approved planned route/schedule information, and returns to the
same search and result set when the passenger navigates back.

**Acceptance Scenarios**:

1. **Given** search results contain a service, **When** the passenger selects it,
   **Then** the app shows its service identity, route identity/name where supplied, ordered
   route stops, the selected travel date, and planned origin/destination times available
   from the search result.
2. **Given** the selected service detail is available, **When** the passenger reviews its
   schedule summary, **Then** any first-call departure is labeled as planned schedule data
   and is not presented as the selected origin's time unless those values are the same.
3. **Given** the passenger is viewing a service detail, **When** they choose Back,
   **Then** the prior search criteria and results are restored.
4. **Given** the service or route is no longer available, **When** its detail cannot be
   retrieved, **Then** the affected detail section is marked unavailable, any separately
   loaded section remains visible, and the passenger can return to retained results.
5. **Given** either service detail or route stops fails while the other read succeeds,
   **When** the detail view is shown, **Then** it presents the successful information and
   marks only the failed section unavailable with a retry for that section.

**Resolved UX decision**: Use documented fields only. The detail view shows service and
route identity, ordered stop names, and planned times available for the searched origin and
destination. It does not show intermediate-stop times because the approved contracts do not
provide them. Adding such times would require a separately approved contract change.

Planned call timestamps are displayed using their supplied local date and clock time. When
a call's calendar date differs from the selected origin travel date, the app labels it
"next day". The app does not convert published schedule times to the device time zone.

---

### User Story 3 - Recover from unavailable information (Priority: P2)

As a passenger, I want clear feedback when railway information cannot be loaded, so I know
whether to correct my search or retry later.

**Why this priority**: Clear recovery prevents backend or network problems from looking like
valid empty results or misleading schedule information.

**Independent Test**: Exercise documented validation, missing-resource, unavailable-service,
server-error, and network-loss outcomes and verify the appropriate message, preserved
criteria, and available recovery action for each.

**Acceptance Scenarios**:

1. **Given** a search request is being processed, **When** the response is pending,
   **Then** a loading state is visible and duplicate submission is prevented or safely
   ignored.
2. **Given** the backend returns a documented validation error, **When** the search fails,
   **Then** the app explains that the criteria need attention, preserves the passenger's
   selections, and allows correction without showing an empty-results state.
3. **Given** railway information is unavailable or the network request fails,
   **When** no current response can be loaded, **Then** the app identifies the information
   as unavailable, preserves the search criteria, and offers a retry action.
4. **Given** the app had displayed data before a network loss during the current session,
   **When** refresh fails, **Then** any retained data is visibly identified as not refreshed
   and is not described as live or newly confirmed.
5. **Given** a retry succeeds, **When** the new response is received, **Then** the loading
   and unavailable indicators are replaced by the corresponding results, empty state, or
   service detail.

## Requirements

### Approved requirement traceability

The following wording and priorities are from the approved requirements inventory, which
transcribes the authoritative workbook. Acceptance checks are quoted exactly.

| SR ID | Approved requirement wording | Priority | Approved acceptance check |
|---|---|---|---|
| SR-002 | The platform shall maintain identifiable railway stations and expose station names and essential location/service details to passenger and operational channels. | Must Have | Station records can be created, retrieved, referenced by services, and displayed. |
| SR-003 | The platform shall represent railway routes and their ordered station sequences. | Must Have | Route stops are returned in journey order with unambiguous station IDs. |
| SR-004 | The platform shall maintain train services associated with routes and identifiable service schedules. | Must Have | Every published service references a valid route, schedule and service identifier. |
| SR-013 | Passengers shall be able to search railway services using an origin, destination and travel date. | Must Have | Valid search returns matching service choices; invalid pairs and no-result searches show useful feedback. |
| SR-089 | Public railway information shall be distinguishable from functions requiring passenger or railway-staff authentication. | Must Have | Anonymous viewer can access permitted service details but not protected operational APIs. |
| SR-109 | Passenger, driver, and station clients shall provide clear network-availability and data-freshness indications when live information cannot be refreshed. | Must Have | Offline simulator shows visible state/last updated time for each applicable client. |
| SR-114 | The system shall provide meaningful loading, no-data, access-denied, error and retry states for primary passenger and operational workflows. | Must Have | Each primary view has tested no-results, 403, 500 and no-network cases. |
| SR-103 | Interactive passenger and station-terminal controls shall have accessible names, focus order and meaningful announcements for relevant changing journey information. | Should Have | Keyboard and screen-reader tests can complete supported journey lookup tasks. |
| SR-104 | Passenger-facing controls shall support legible typography, sufficient contrast, usable touch targets and text scaling appropriate to target devices. | Should Have | At increased text scale, essential information remains usable and controls remain operable. |

For SR-104, the approved delivery limit is "Not committed this week. Attempt only after
all 5 critical flows pass." SR-103 and SR-104 remain Should Have stretch requirements;
this specification does not reclassify them as Must Have.

SR-002, SR-003, and SR-004 describe platform data ownership and backend outcomes. This
feature covers only passenger selection and presentation of those published records; it
does not implement station administration, route maintenance, or schedule management.
SR-109 applies to connection availability and last successful retrieval in this feature;
the scheduled data itself is not represented as live information.

### Functional Requirements

- **FR-001**: The app MUST allow passengers to open railway search and view approved public
  station, route, service, and schedule information without signing in.
- **FR-002**: The app MUST let a passenger select an origin from available published
  stations and show the station name plus only approved location/service details supplied
  for that station.
- **FR-003**: The app MUST let a passenger select a destination from available published
  stations and MUST prevent the selected origin and destination from being identical.
- **FR-004**: The app MUST let a passenger select one travel date and MUST submit that
  calendar date in the contract's full-date form without shifting it across calendar days.
- **FR-005**: The app MUST submit a search only when origin, destination, and travel date are
  present and the two station identifiers differ. Backend date eligibility remains
  authoritative; the app MUST NOT calculate service operating dates or route eligibility.
- **FR-006**: The app MUST present each returned service choice using its supplied service
  and route identifiers/codes and planned origin/destination calls, preserving the meaning
  and values of the response.
- **FR-007**: The app MUST show an explicit no-results state for a successful response with
  an empty services collection and MUST distinguish it from validation, access, network, or
  backend errors.
- **FR-008**: The app MUST allow a passenger to open a result and request its service detail
  and ordered route stops using the identifiers supplied in that result. It MUST present
  only fields available from the approved contracts; intermediate-stop schedule times MUST
  NOT be invented.
- **FR-009**: The app MUST label service times as planned/scheduled information, display the
  local date and time represented by each offset-bearing scheduled call, and MUST NOT present
  them as observed, estimated, or live train status. If a call's date differs from the
  selected origin travel date, the app MUST identify it as a later-day call (for example,
  "next day"). The app MUST NOT convert the published local schedule clock time to device
  time.
- **FR-010**: The app MUST provide clear loading feedback while station, search, route-stop,
  or service-detail information is being retrieved.
- **FR-011**: For a documented 400 response, the app MUST retain entered criteria, show a
  useful validation message, and allow correction. It MUST use specific field feedback only
  when the response identifies that field; otherwise it MUST use a general criteria message.
- **FR-012**: For a documented 404 response when a selected service or route is unavailable,
  the app MUST explain that the affected information could not be found, preserve any
  separately loaded service or route information, and provide a path back to results or
  search.
- **FR-013**: For a documented 503 response, an unexpected server error including 500, or a
  network failure, the app MUST show an unavailable state for the affected information,
  preserve the passenger's search criteria and any independently loaded detail section, and
  provide a retry action for the failed read. It MUST NOT describe these failures as a valid
  empty result.
- **FR-014**: For a 403 or other access-denied response, the app MUST show a clear access
  unavailable state for the affected information without prompting the passenger to sign in
  for these approved public reads. Any independently loaded section MUST remain visible.
- **FR-015**: A retry MUST repeat the failed read using the retained selection. When one of
  the independent service-detail or route-stop reads failed, the retry MUST target that read
  without discarding successfully loaded information. On success, the app MUST replace the
  transient error/loading state with the returned data or explicit empty state.
- **FR-016**: If previously retrieved information remains visible after a refresh fails,
  the app MUST identify it as not refreshed and, where known, show when it was last
  successfully retrieved. This timestamp MUST NOT be represented as the source's update time.
- **FR-017**: Back navigation from service detail MUST return to the preceding search results
  with the selected criteria retained. Returning from results to search MUST retain the
  criteria for editing.
- **FR-018**: Search fields, date selection, result choices, retry controls, and changing
  status messages SHOULD expose understandable accessible names, states, focus order, and
  assistive-technology announcements, consistent with SR-103's approved Should Have priority.
- **FR-019**: Search, results, and detail content SHOULD remain understandable with text
  scaling and MUST NOT rely on color alone to convey errors, availability, or selection.
  Text-scaling and control-operability checks retain SR-104's approved Should Have priority.
- **FR-020**: The app MUST use the documented public Railway Network reads: `GET /api/stations`,
  `GET /api/routes/{id}/stops`, `GET /api/services?origin=&destination=&date=`, and
  `GET /api/services/{id}`. Their approved OpenAPI schemas and response semantics are the
  integration authority. The app MUST NOT add write operations or infer undocumented
  response behavior.
- **FR-021**: When a passenger edits origin, destination, or travel date after results are
  displayed, the app MUST clear those results immediately, retain the edited criteria, and
  require a new explicit search before displaying results for the changed criteria.

### Edge Cases

- The station list is empty: show that station choices are unavailable and prevent search
  until stations can be loaded.
- Station list retrieval fails: show an unavailable state and retry; do not offer fabricated
  or stale station records as current.
- Origin equals destination: identify the conflict and require a different station before
  submitting.
- A station identifier becomes invalid or unpublished between selection and search: treat
  the documented 400 response as a correctable criteria error.
- The valid search returns an empty array: show no matching published services for the
  selected criteria.
- A returned service disappears before details load: treat 404 as unavailable detail and
  allow return to results.
- Service-detail data fails while route stops load: show the available search summary and
  route stops, mark only service detail unavailable, and allow retry of that read.
- Route-stop data is missing or unavailable while service detail loads: preserve service
  identity and planned schedule data, mark only route stops unavailable, and allow retry of
  the route-stop read; do not fabricate a route sequence.
- The route response contains no stops: show the route identity with an explicit absence of
  published stop details.
- The user changes origin after choosing a destination and the pair becomes identical:
  revalidate the pair before enabling successful submission.
- The user edits any search criterion while results are visible: clear the old result list
  immediately so it cannot be mistaken for a response to the edited criteria.
- A request is repeated while one is pending: prevent duplicate visible results or duplicate
  navigation.
- Device connectivity is lost during search or detail retrieval: show network unavailable,
  preserve criteria, and allow retry after connectivity returns.
- A backend error contains technical details: show safe, passenger-appropriate text; do not
  expose stack traces or internal diagnostics.
- A scheduled timestamp includes an offset: display its local schedule date and clock time;
  identify a destination call on a later date as "next day" and do not convert it to device
  time or imply it is an observed time.

### Scope Boundaries

In scope: anonymous public station selection, origin/destination/date search, scheduled
service results, planned service/route details supported by approved contracts, navigation,
accessibility, and read-failure feedback.

Out of scope: live journey tracking, official disruption publication, passenger
authentication implementation, food ordering, ticket purchasing, driver operations, and
incident management. This feature does not implement backend data creation or edit flows.

### Key Entities

- **Station**: A published railway station identified by a stable ID, name, and any approved
  location or service details; used as a search origin or destination.
- **Service search criteria**: The chosen origin station ID, destination station ID, and
  travel date for one passenger search.
- **Scheduled service choice**: A published service identity and route reference with the
  planned calls at the selected origin and destination for the requested date.
- **Route stop**: A station on a route identified by station ID and canonical stop order;
  schedule times for intermediate stops are not provided by the current route-stop contract.
- **Service detail**: A published service and route identity plus the planned schedule
  summary supplied by the service-detail contract.

## Success Criteria

### Measurable Outcomes

- **SC-001**: In all valid-search acceptance cases, the displayed origin, destination, date,
  service identity, and planned call values match the successful backend response.
- **SC-002**: In all tested same-station or incomplete-criteria cases, no search is submitted
  and the passenger receives an actionable validation message.
- **SC-003**: In all tested 200-empty, 400, 403, 404, 500, 503, and no-network cases, the app
  shows the corresponding empty, validation, access, missing-resource, or unavailable state
  rather than conflating failures with no results.
- **SC-004**: In all successful service-detail cases, route stops appear in backend order,
  each displayed time retains its scheduled local date and clock time, later-day calls are
  identified as "next day", and every time is identified as planned schedule information.
- **SC-007**: In every partial detail failure case, the successful section remains visible,
  the failed section is identified accurately, and retrying it does not discard successful
  information.
- **SC-005**: Passengers can move from search to results to a service detail and back while
  retaining the same search criteria and result list in every acceptance run.
- **SC-006**: Every interactive search/result/retry control has a meaningful accessible
  name, and loading, empty, unavailable, and result changes are available to assistive
  technology in the accessibility review.

## Assumptions

- The approved Railway Network API is reachable through an environment-specific base URL
  provided outside this feature specification; this feature does not select or hardcode a
  production address.
- The API owns date eligibility, station publication, service publication, and timetable
  correctness. The app sends passenger criteria and presents returned data.
- Search result order and station list order are preserved as returned because the current
  contracts do not define a client sorting rule.
- Back navigation retains criteria and results for the current search session.
- If a network refresh fails while previously loaded information is still visible, the app
  can label it with the local time of the last successful retrieval. It does not claim that
  this is the railway source's update time.
- The current contracts do not provide intermediate stop schedule times. The UX decision
  recorded under User Story 2 is resolved by using documented fields only.
- No approved maximum date range or past-date restriction is defined in the cited search
  contract; date eligibility feedback is owned by the backend response.

## Dependencies

- Approved public Railway Network reads for station listing, route stops, date-based service
  search, and service detail.
- Approved requirement source and handoff documents for the accepted passenger scope and
  cross-cutting behavior.
- Current service and route contracts; intermediate-stop scheduled times are outside this
  feature unless a later contract change is approved.
