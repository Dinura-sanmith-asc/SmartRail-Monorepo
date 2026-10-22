# SmartRail Accessibility Review

**Findings:** 7

## ACCESSIBILITY-001 — Screen Reader Text Alternatives and Live Status Updates for Passenger Displays

**Category:** ACCESSIBILITY_GAP
**Severity:** HIGH
**Area:** Station passenger-information displays and Station Terminal Application

**Finding:** Station digital displays and interactive public terminals are planned without explicit requirements for text alternatives, aria-live region semantics for train time updates, or audio announcements for platform and departure changes.

**Candidate requirement:** All station passenger-information displays and interactive terminals must provide accessible text descriptions, semantic ARIA live regions for real-time train status changes, and synchronized audio-visual equivalents for crucial announcements like platform and departure time modifications.

**Sources:** BRIEF-6, BRIEF-7, AD-016

**Question:** How do visually impaired passengers receive real-time updates when platform changes or delays occur on station screens?

**Verification:** Test interactive station terminals using a screen reader to verify that dynamic arrivals, departures, and status updates trigger aria-live announcements without user focus loss.

**Architect status:** PENDING REVIEW

## ACCESSIBILITY-002 — Keyboard and Switch Device Navigation for Station Terminals

**Category:** ACCESSIBILITY_GAP
**Severity:** HIGH
**Area:** Station Terminal Application

**Finding:** Interactive station kiosks lack defined requirements for full hardware or software keyboard navigation, focus indicators, and alternative input mechanisms suitable for passengers with mobility impairments.

**Candidate requirement:** Station interactive terminals must be fully operable via an external keyboard or accessible assistive switch interface, maintaining clear, high-contrast focus indicators across all route search and journey exploration elements.

**Sources:** BRIEF-7, BRIEF-15

**Question:** Can a passenger with severe upper-body mobility impairments complete a journey search on the station terminal using only keyboard or switch inputs?

**Verification:** Perform a keyboard-only audit of the station terminal application to confirm that all interactive elements are reachable, operable, and exhibit visible focus rings.

**Architect status:** PENDING REVIEW

## ACCESSIBILITY-003 — Color Contrast and Non-Color Status Indicators for Disruption Feeds

**Category:** ACCESSIBILITY_GAP
**Severity:** HIGH
**Area:** Passenger mobile application and Station passenger-information displays

**Finding:** Disruptions, delays, and service cancellations are frequently indicated using color alone (e.g., red for late, green for on-time), which fails WCAG 2.2 AA non-color status requirements for color-blind passengers.

**Candidate requirement:** All status indicators showing train delays, platform alterations, or cancellations must combine color with distinct text labels, icons, or patterns to ensure comprehension without relying solely on color perception.

**Sources:** BRIEF-3, BRIEF-10, BRIEF-15

**Question:** How can a user with severe deuteranopia distinguish between a cancelled train and a delayed train if color is the only indicator?

**Verification:** Inspect UI mockups and live application views using a grayscale simulator or contrast analysis tool to confirm that all status states include explicit text tags and icons alongside color.

**Architect status:** PENDING REVIEW

## ACCESSIBILITY-004 — Text Scaling, Touch-Target Sizing, and Readability on Public Terminals

**Category:** ACCESSIBILITY_GAP
**Severity:** MEDIUM
**Area:** Station Terminal Application and Passenger mobile application

**Finding:** Public terminals and mobile apps lack explicit sizing criteria for touch targets and dynamic text reflow, which poses usability challenges for elderly users and passengers with low vision.

**Candidate requirement:** Touch targets on station terminals and mobile apps must be at least 48x48 CSS pixels, and text must support scaling up to 200 percent without breaking layout reflow or causing horizontal scrolling.

**Sources:** BRIEF-3, BRIEF-7, BRIEF-15

**Question:** Are touch targets large enough and text scalable enough for elderly passengers interacting quickly with public station terminals in high-stress environments?

**Verification:** Execute automated and manual layout testing at 200% text zoom and measure interactive button dimensions to verify compliance with minimum touch target requirements.

**Architect status:** PENDING REVIEW

## ACCESSIBILITY-005 — Timeout Handling and Extended Interaction Windows on Public Terminals

**Category:** ACCESSIBILITY_GAP
**Severity:** HIGH
**Area:** Station Terminal Application

**Finding:** Interactive public kiosks do not account for elderly users or passengers with cognitive or physical disabilities who require additional time to read journey schedules or complete searches before session timeouts occur.

**Candidate requirement:** Station interactive terminals must provide clear, accessible visual warnings prior to session expiration and allow users to easily extend their interaction session with a single accessible action.

**Sources:** BRIEF-7, BRIEF-15

**Question:** What prevents a public terminal from abruptly clearing a slow-moving elderly passenger's custom route search mid-interaction?

**Verification:** Simulate a slow user journey on the station terminal emulator and verify that a warning dialog appears with sufficient time extension options before session reset.

**Architect status:** PENDING REVIEW

## ACCESSIBILITY-006 — Offline and Degraded Accessibility Information Synchronization

**Category:** ACCESSIBILITY_GAP
**Severity:** MEDIUM
**Area:** Passenger mobile application and Station infrastructure

**Finding:** While offline caching is planned for operations and basic services, there is no mandate to ensure verified accessibility facility statuses (such as lift or ramp outages) are cached locally and marked as stale during network loss.

**Candidate requirement:** When station connectivity is lost, passenger and terminal applications must display the last known verified accessibility status of station lifts and ramps, clearly timestamped and explicitly marked as cached or potentially stale data.

**Sources:** BRIEF-16, AD-025, AD-027, AD-052

**Question:** How does a wheelchair user relying on the mobile app know if elevator status information is live or based on outdated cached data during a network outage?

**Verification:** Simulate network disconnection in the passenger mobile app and verify that facility status indicators display explicit staleness warnings and last-updated timestamps.

**Architect status:** PENDING REVIEW

## ACCESSIBILITY-007 — Driver Application Accessibility and High-Contrast Operational Displays

**Category:** ACCESSIBILITY_GAP
**Severity:** MEDIUM
**Area:** Driver and operations applications

**Finding:** The Driver / Train Operator application lacks defined requirements for high-contrast legibility, glare reduction, and non-distracting visual or audio alerts under varied lighting conditions in the cab.

**Candidate requirement:** The driver operational application must support high-contrast display modes, scalable text, and multi-modal alerts (auditory combined with visual) to ensure safety-critical notifications are perceptible to operators with varying visual acuity.

**Sources:** BRIEF-8, BRIEF-15, AD-028, AD-033

**Question:** Can a train operator easily read assigned journey stops and safety notices under bright sunlight or low-light cabin conditions without visual fatigue?

**Verification:** Evaluate the driver application UI against high-contrast standards and verify that critical safety notifications trigger distinct, configurable audio-visual alerts requiring manual acknowledgment.

**Architect status:** PENDING REVIEW

## Overall Observations

- The architecture successfully bounds scope by avoiding individual assistance-tracking workflows, but this decision must not exempt core passenger-facing features from robust accessibility compliance.
- Station terminals, mobile apps, and digital displays require rigorous adherence to WCAG 2.2 AA guidelines to support diverse commuter demographics, including elderly passengers and users with sensory or physical disabilities.
- Degraded and offline states must maintain transparent communication regarding facility and accessibility status to prevent stranded passengers from relying on stale data.