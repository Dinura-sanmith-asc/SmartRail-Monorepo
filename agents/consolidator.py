import json
from typing import Literal, Optional

from pydantic import BaseModel, Field

from agents.base import run_structured_agent


FindingType = Literal[
    "AGREEMENT",
    "OVERLAP",
    "CONFLICT",
    "OWNERSHIP_AMBIGUITY",
    "OPEN_QUESTION",
    "SCOPE_TENSION",
    "UNIQUE_IMPORTANT_FINDING",
]


class ConsolidatedFinding(BaseModel):
    finding_type: FindingType

    title: str = Field(
        description="Short descriptive title."
    )

    summary: str = Field(
        description=(
            "Clear explanation of the cross-stakeholder finding. "
            "Do not turn it into a final requirement."
        )
    )

    source_refs: list[str] = Field(
        description=(
            "IDs of source discoveries supporting this finding, "
            "for example PASSENGER-003 or DRIVER-011."
        )
    )

    stakeholders: list[str]

    categories: list[str]

    unresolved_question: Optional[str] = Field(
        default=None,
        description=(
            "Question requiring stakeholder or architect decision."
        )
    )

    workshop_required: bool = Field(
        description=(
            "True when this issue should be discussed in a "
            "cross-stakeholder workshop."
        )
    )

    rationale: str = Field(
        description=(
            "Why these source findings belong together and why "
            "the classification was selected."
        )
    )


class ConsolidationReport(BaseModel):
    total_source_discoveries: int

    consolidated_findings: list[ConsolidatedFinding]

    cross_cutting_observations: list[str]


ROLE = """
You are the Cross-Stakeholder Requirements Consolidator
for SmartRail.

You receive structured discoveries from multiple independent
stakeholder interviews.

Your responsibility is to compare them and identify
relationships between stakeholder findings.

Use these classifications:

AGREEMENT
- two or more stakeholders independently express a compatible need

OVERLAP
- findings describe substantially the same concern from
  different perspectives

CONFLICT
- stakeholders express incompatible expectations or rules

OWNERSHIP_AMBIGUITY
- multiple roles appear responsible for the same information,
  decision, action, or process and authority is unclear

OPEN_QUESTION
- an important question remains unanswered

SCOPE_TENSION
- a capability appears useful but may significantly increase
  the project or MVP scope

UNIQUE_IMPORTANT_FINDING
- an important discovery appears only from one stakeholder
  and must not be lost simply because nobody else mentioned it


IMPORTANT RULES:

1. Do NOT create final SmartRail requirements.

2. Do NOT make architecture decisions.

3. Do NOT decide which stakeholder is correct.

4. Do NOT resolve conflicts.

5. Preserve source traceability using the supplied discovery IDs.

6. Never invent source IDs.

7. Do not merge findings merely because they have the same
   category.

8. Merge findings only when they genuinely refer to the same
   underlying concern.

9. Preserve important safety, accessibility, security,
   operational, integration, and failure findings even if
   only one stakeholder mentioned them.

10. If responsibility is unclear, classify it as
    OWNERSHIP_AMBIGUITY rather than silently assigning
    ownership.

11. If stakeholders disagree, represent the disagreement
    neutrally.

12. If a finding needs human judgment, set
    workshop_required=true.

13. Ticketing, payments, compensation, seat reservations,
    indoor navigation, crowd information, lost property,
    onward transport integration and similar large features
    should be treated carefully as potential SCOPE_TENSION
    unless the evidence clearly shows otherwise.

14. The architect will make final decisions later.
"""


def consolidate(discoveries):
    serialized = json.dumps(
        discoveries,
        indent=2,
        ensure_ascii=False,
    )

    question = f"""
Below are structured requirement-discovery findings collected
from independent SmartRail stakeholder interviews.

Compare them across stakeholders.

Identify agreements, overlaps, conflicts, ownership ambiguity,
open questions, scope tensions, and important unique findings.

Do not resolve any issue.

SOURCE DISCOVERIES:
-------------------
{serialized}
-------------------

Return a structured ConsolidationReport.
"""

    return run_structured_agent(
        role=ROLE,
        question=question,
        schema=ConsolidationReport,
    )