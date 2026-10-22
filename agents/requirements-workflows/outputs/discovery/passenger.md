# Passenger Discovery Analysis

## 1. Unified Journey Planner

- **Category:** SCOPE_CANDIDATE
- **Origin:** EXPLICIT
- **Discovery:** A single national platform that aggregates all routes, schedules, and real-time prices from all operators to help passengers find and compare options.
- **Source basis:** Stage 1 outlines a unified journey planner aggregating routes, schedules, and prices.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 2. Transparent Fares Display

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** Clearly display all ticket types, their terms, and the total cost upfront, including applicable railcard discounts.
- **Source basis:** Stage 1 and 2 mention transparent cost estimates, total cost, and automatic application of registered railcard discounts.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** How will SmartRail manage existing multi-operator fare structures and potential split-ticketing options to ensure passengers get the best value?

## 3. Detailed Accessibility Information and Filtering

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** Integrate comprehensive, verified accessibility information for every station and rolling stock type, enabling passengers to filter by specific needs such as step-free access or accessible toilets.
- **Source basis:** Stage 1 details accessibility information and filtering requirements.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** What level of granular detail will accessibility information provide (e.g., specific measurements vs. general tags)?

## 4. Proactive Disruption Warnings

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** Highlight planned engineering works or long-term disruptions upfront during the planning stage and suggest suitable alternatives.
- **Source basis:** Stage 1 highlights the need for proactive disruption warnings and engineering work summaries.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** How frequently will engineering work schedules be updated and verified by SmartRail from all operators?

## 5. Service and Occupancy Details

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** Display information on available on-board services like WiFi, catering, and power sockets, along with estimated train occupancy levels.
- **Source basis:** Stage 1 mentions displaying on-board services and occupancy levels.
- **Scope signal:** EXTENSION_CANDIDATE
- **Open question:** Will SmartRail provide predictive occupancy levels based on historical data and current booking trends, or just current real-time data?

## 6. Integrated Onward Travel Information

- **Category:** SCOPE_CANDIDATE
- **Origin:** EXPLICIT
- **Discovery:** Provide basic information about local transport links at origin and destination stations.
- **Source basis:** Stage 1 notes integrated onward travel and local transport links.
- **Scope signal:** EXTENSION_CANDIDATE
- **Open question:** None

## 7. Multi-operator Journey Handling

- **Category:** AMBIGUITY
- **Origin:** EXPLICIT
- **Discovery:** Uncertainty regarding how SmartRail will handle scenarios where an optimal journey involves travel with multiple operators that do not traditionally integrate ticketing.
- **Source basis:** Stage 1 asks how SmartRail will handle non-integrated multi-operator journeys.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** How will SmartRail handle scenarios where an optimal journey involves travel with multiple operators that don't traditionally integrate their ticketing?

## 8. Visual Seat Selection

- **Category:** SCOPE_CANDIDATE
- **Origin:** EXPLICIT
- **Discovery:** Allow passengers to select specific seats from a visual coach layout highlighting amenities, accessible spaces, and proximity to toilets.
- **Source basis:** Stage 2 discusses options to select specific seats and visual seat maps.
- **Scope signal:** EXTENSION_CANDIDATE
- **Open question:** None

## 9. Special Assistance Integration

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** Provide a dedicated section within the booking process to request and confirm special assistance directly with clear confirmation messages.
- **Source basis:** Stage 2 describes assistance integration during booking.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** Will the system differentiate between different types of assistance needs and tailor the request accordingly?

## 10. Direct Retailer vs. Redirect Role

- **Category:** AMBIGUITY
- **Origin:** EXPLICIT
- **Discovery:** It is unclear whether SmartRail will act as a direct retailer for all train tickets or redirect users to individual operator sites.
- **Source basis:** Stage 2 asks if SmartRail will act as a direct retailer or redirect to individual operator sites.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** Will SmartRail act as a direct retailer for all train tickets, or will it redirect to individual operator sites for final purchase?

## 11. Data Security and Privacy Protocols

- **Category:** SECURITY
- **Origin:** EXPLICIT
- **Discovery:** Protocols must be defined for data security and privacy when storing passenger payment details and personal information.
- **Source basis:** Stage 2 question about data security and privacy protocols for payment and personal details.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** What protocols will be in place for data security and privacy when storing payment details and personal information?

## 12. Proactive Notifications for Day of Travel

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** Send push notifications for significant delays, cancellations, or platform changes affecting a specific booked journey.
- **Source basis:** Stage 3 outlines proactive notifications for day of travel updates.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** What threshold constitutes a significant delay for a push notification and can passengers customize this?

## 13. Signal Loss and Device Failure

- **Category:** FAILURE_SCENARIO
- **Origin:** EXPLICIT
- **Discovery:** Passengers facing phone battery failure, missing tickets, or signal loss at the station when trying to present mobile tickets.
- **Source basis:** Stage 3 mentions ticket troubles such as dying phone batteries or missing tickets, and asks about offline mode.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** Will SmartRail have an offline mode for displaying mobile tickets in case of signal loss at the station?

## 14. Interactive Station Maps and Coach Stopping Positions

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** Provide interactive, GPS-enabled station maps showing facilities, assistance points, accessible routes, and estimated coach stopping positions on the platform.
- **Source basis:** Stage 4 covers interactive station maps and estimated coach stopping positions.
- **Scope signal:** EXTENSION_CANDIDATE
- **Open question:** What is the accuracy level of coach stopping position predictions given varied train lengths and platform designs?

## 15. Facility and Lift Out-of-Order Alerts

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** Real-time alerts for out-of-order station facilities such as lifts, escalators, and accessible toilets.
- **Source basis:** Stage 4 mentions facility alerts for out-of-order station facilities.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** How will SmartRail ensure that real-time lift/escalator status is consistently and accurately reported from all stations?

## 16. Real-Time Train Tracking and In-Journey Updates

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** A live map showing the train's current location, speed, and estimated arrival times, alongside push notifications for delays and disruption explanations.
- **Source basis:** Stage 5 specifies real-time train tracking and proactive in-journey updates.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** What level of detail will be provided in disruption explanations and how will this be communicated consistently?

## 17. Discreet Issue Reporting on Board

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** A discreet feature allowing passengers to report non-emergency issues like heating problems or anti-social behavior directly to on-board staff.
- **Source basis:** Stage 5 mentions reporting non-emergency issues directly to on-board staff.
- **Scope signal:** EXTENSION_CANDIDATE
- **Open question:** What is the service level agreement for responses to issues reported via the app?

## 18. Missed Connections and Connection Risks

- **Category:** FAILURE_SCENARIO
- **Origin:** EXPLICIT
- **Discovery:** Delays on initial trains putting connections at risk or causing missed connections without clear alternative guidance or protected status.
- **Source basis:** Stage 6 identifies risks of missed connections, unprotected connections, and lack of guidance.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** Will SmartRail clearly indicate if a connection is protected and what level of automatic re-booking will be facilitated?

## 19. Centralized Compensation Claims (Delay Repay)

- **Category:** BUSINESS_PROCESS
- **Origin:** EXPLICIT
- **Discovery:** A centralized, simplified process for claiming Delay Repay, potentially auto-populating journey details from booking history.
- **Source basis:** Stage 8 details simplified compensation and centralized Delay Repay claims.
- **Scope signal:** EXTENSION_CANDIDATE
- **Open question:** Will SmartRail directly manage compensation claims or redirect to operators, and how will dispute resolution be handled?

## 20. Consolidated Lost Property Search

- **Category:** SCOPE_CANDIDATE
- **Origin:** EXPLICIT
- **Discovery:** A consolidated lost property search portal across all participating operators to track and retrieve lost items.
- **Source basis:** Stage 8 notes centralized lost property search portal.
- **Scope signal:** EXTENSION_CANDIDATE
- **Open question:** Will the lost property portal require real-time updates from all operators, and how will its effectiveness be measured?
