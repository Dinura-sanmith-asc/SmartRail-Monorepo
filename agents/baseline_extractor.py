
import json
from typing import Literal, Optional

from pydantic import BaseModel, Field

from agents.base import run_structured_agent


class BaselineItem(BaseModel):
    source_ref: str
    title: str

    statement: str = Field(
        description="One clear and atomic requirement or obligation."
    )

    classification: Literal[
        "PRODUCT_FUNCTIONAL",
        "PRODUCT_NON_FUNCTIONAL",
        "PRODUCT_DATA",
        "PRODUCT_INTEGRATION",
        "ASSESSMENT_DELIVERABLE",
        "ENGINEERING_PROCESS",
    ]

    source_strength: Literal[
        "EXPLICIT",
        "TO_BE_DEFINED",
        "ILLUSTRATIVE",
    ]

    evidence_excerpt: str

    clarification_question: Optional[str] = None


class BaselineReport(BaseModel):
    items: list[BaselineItem]


ROLE = """
You are the SmartRail Baseline Requirements Analyst.

You receive sections of the ORIGINAL SmartRail assessment.

Extract all distinct requirements and engineering obligations.

IMPORTANT:

The assessment is an incomplete business brief, not a
complete requirements specification.

You MUST distinguish:

EXPLICIT:
The brief clearly expects this capability or deliverable.

TO_BE_DEFINED:
The brief requires the architect to determine details.

ILLUSTRATIVE:
The brief gives an example or possibility, not a mandatory
implementation commitment.

Classify each item into:

- PRODUCT_FUNCTIONAL
- PRODUCT_NON_FUNCTIONAL
- PRODUCT_DATA
- PRODUCT_INTEGRATION
- ASSESSMENT_DELIVERABLE
- ENGINEERING_PROCESS

Rules:

1. Preserve all meaningful baseline obligations.
2. Do not silently treat examples as mandatory features.
3. Do not invent requirements beyond the supplied sections.
4. Do not make architecture decisions.
5. Do not prioritize.
6. Do not make MVP scope decisions.
7. Do not merge unrelated requirements.
8. Keep statements atomic and understandable.
9. Preserve the exact supplied BRIEF-N source reference.
10. Include a short supporting excerpt from the source.
11. Record unresolved definitions as clarification questions.

This task is extraction, NOT final requirement refinement.
"""


def extract_baseline(sections):
    question = f"""
Extract the requirements and obligations from these
original assessment sections.

Every section is labeled with its source reference.

SOURCE SECTIONS:

{json.dumps(sections, indent=2, ensure_ascii=False)}
"""

    return run_structured_agent(
        role=ROLE,
        question=question,
        schema=BaselineReport,
    )
