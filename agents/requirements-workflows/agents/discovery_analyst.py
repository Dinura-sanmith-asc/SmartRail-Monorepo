from typing import Literal, Optional

from pydantic import BaseModel, Field

from agents.base import run_structured_agent


DiscoveryCategory = Literal[
    "MISSING_ACTOR",
    "FUNCTIONAL",
    "BUSINESS_PROCESS",
    "STATE",
    "FAILURE_SCENARIO",
    "SECURITY",
    "OPERATIONAL",
    "ACCESSIBILITY",
    "INTEGRATION",
    "CONFLICT",
    "AMBIGUITY",
    "ASSUMPTION",
    "SCOPE_CANDIDATE",
]


DiscoveryOrigin = Literal[
    "EXPLICIT",
    "CLEAR_INFERENCE",
]


ScopeSignal = Literal[
    "CORE_CANDIDATE",
    "EXTENSION_CANDIDATE",
    "REVIEW_NEEDED",
]


class DiscoveryItem(BaseModel):
    category: DiscoveryCategory

    title: str = Field(
        description="Short descriptive title for the discovery."
    )

    discovery: str = Field(
        description=(
            "Clear description of what was discovered. "
            "Do not convert it into a final requirement."
        )
    )

    source_basis: str = Field(
        description=(
            "Short paraphrase explaining what in the stakeholder "
            "interview supports this discovery."
        )
    )

    origin: DiscoveryOrigin

    open_question: Optional[str] = Field(
        default=None,
        description=(
            "Important unresolved question if clarification "
            "is still required."
        ),
    )

    scope_signal: ScopeSignal = Field(
        description=(
            "Preliminary indication only. This must NOT be treated "
            "as the final scope decision."
        )
    )


class DiscoveryReport(BaseModel):
    stakeholder: str
    discoveries: list[DiscoveryItem]


ROLE = """
You are the Requirements Discovery Analyst for SmartRail.

You are NOT a stakeholder.

Your responsibility is to analyse the output of a simulated
stakeholder interview and extract useful requirement-discovery
findings.

You must identify findings using these categories:

- MISSING_ACTOR
- FUNCTIONAL
- BUSINESS_PROCESS
- STATE
- FAILURE_SCENARIO
- SECURITY
- OPERATIONAL
- ACCESSIBILITY
- INTEGRATION
- CONFLICT
- AMBIGUITY
- ASSUMPTION
- SCOPE_CANDIDATE

Important rules:

1. Analyse only the stakeholder interview provided to you.

2. Do not invent functionality that is unrelated to the
   interview.

3. Do not turn every stakeholder wish into a final system
   requirement.

4. Distinguish between:
   - something explicitly mentioned by the stakeholder
   - something that is a clear inference from their discussion

5. Do not resolve ambiguities or conflicts.

6. Do not decide final project scope.

7. If the stakeholder proposes something potentially large,
   such as ticket purchasing, compensation, indoor navigation,
   seat reservations, or similar functionality, capture it as
   a scope candidate rather than automatically accepting it.

8. Preserve important failure scenarios and unusual situations.

9. Preserve unanswered questions.

10. Avoid duplicate findings within the same stakeholder
    analysis.

The architect will review and make the final decisions later.
"""


def analyze(stakeholder, interview):
    question = f"""
STAKEHOLDER:
{stakeholder}

STAKEHOLDER INTERVIEW:
----------------------
{interview}
----------------------

Analyse this stakeholder interview.

Extract all meaningful requirement-discovery findings.

Do not resolve them.

Return a structured DiscoveryReport.
"""

    return run_structured_agent(
        role=ROLE,
        question=question,
        schema=DiscoveryReport,
    )