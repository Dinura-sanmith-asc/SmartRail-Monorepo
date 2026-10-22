# Research and Decisions ? Driver Operations

Source inspection: constitution; workbook `FINAL UNIFIED REQUIREMENTS` rows SR-064/065/066/069/072; approved RBAC/API PDF pages 1?2; Feature 002 authorization handler and contracts; Feature 003 read projections and `IDriverObservationVerifier`. The PDF lists exactly the three paths in OpenAPI.

| Decision | Rationale | Rejected alternative |
|---|---|---|
| Persistent assignment lifecycle + immutable audit; restricted non-HTTP maintenance, production default-deny | Minimal authoritative prerequisite for own-duty and observation Must Haves | Fake assignment provider, HR workflow or public administration API |
| Driver sees own; Controller sees all via existing assignment grant | Matches PDF own/global distinction without a new selector surface | Caller-supplied subject that could widen scope |
| Plain text observation `{requestId,text}` only; 1?4000 trimmed characters | Minimal evidence without invented operational state vocabulary; bounded transport input | Status mutation, platform taxonomy, client actor/timestamp |
| Server UTC report time and captured journey revision | Authorship/provenance distinguish observation from official status | Client-provided official timestamp |
| Authorization before retry result; unique subject/request key | A revoked caller cannot recover protected results from an idempotency cache | Caching authorization or blind replay |
| Existing journey projections, current read each time | Central changes appear on next read, overnight data stays owner-defined | Duplicated official status in Driver tables |
| Genuine assignment/observation adapters | Existing Feature 002/003 ports become operational with real evidence | Production test doubles or changing grants |
| Original assignment must remain active for reference verification; captured revision must exist | Current authoritative owner evidence fails closed; history is not erased | Age cutoff or treating newer journey revision alone as stale |

User explicitly authorized simplest secure autonomous choices within approved scope on 2026-10-20. These detailed schema choices refine existing operations without adding business features. SR-009 remains stretch-only except minimal assignment prerequisite for direct Must Haves. No unresolved implementation design question requires invented data; operational/client acceptance gates are in the specification.
