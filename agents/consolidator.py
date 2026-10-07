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

    title: str

    summary: str

    source_refs: list[str] = Field(
        min_length=1,
        description=(
            "Original discovery IDs supporting this finding."
        ),
    )

    stakeholders: list[str]

    categories: list[str]

    unresolved_question: Optional[str] = None

    workshop_required: bool

    rationale: str


class ConsolidationReport(BaseModel):
    total_source_discoveries: int

    consolidated_findings: list[ConsolidatedFinding]

    cross_cutting_observations: list[str]


BATCH_ROLE = """
You are a SmartRail Requirements Consolidator.

You are processing ONE SMALL BATCH of discoveries.

Your main responsibility is LOSSLESS CONSOLIDATION.

You may group discoveries only when they genuinely concern
the same underlying issue.

CRITICAL COVERAGE RULE:

EVERY source_ref supplied to you MUST appear in at least one
output finding.

If a discovery does not meaningfully combine with another
discovery, preserve it as a UNIQUE_IMPORTANT_FINDING.

Never drop a discovery because it seems:

- minor
- low priority
- out of scope
- duplicated
- unrealistic
- inconvenient
- less important

Scope decisions will be made later by the architect.

Classifications:

AGREEMENT
Compatible stakeholder expectations.

OVERLAP
Similar concern described from different viewpoints.

CONFLICT
Incompatible stakeholder expectations.

OWNERSHIP_AMBIGUITY
Responsibility or authority is unclear.

OPEN_QUESTION
Important unresolved question.

SCOPE_TENSION
Potentially valuable capability that may substantially
increase project scope.

UNIQUE_IMPORTANT_FINDING
Important discovery that should remain independent.

Rules:

1. Do not create final requirements.
2. Do not make architecture decisions.
3. Do not resolve conflicts.
4. Do not decide project scope.
5. Never invent source_ref values.
6. Preserve all original source_refs.
7. If uncertain whether two findings should merge,
   keep them separate.
"""


MERGE_ROLE = """
You are the final Cross-Stakeholder Consolidator for SmartRail.

You receive ALREADY VALIDATED intermediate findings.

Your job is to identify genuine relationships across those
findings.

You may merge findings when they clearly concern the same
underlying issue.

CRITICAL RULE:

EVERY original source_ref contained anywhere in the input
must remain represented in at least one final finding.

When merging multiple findings, preserve ALL of their
source_refs.

Do not drop evidence simply because:

- another finding sounds similar
- the feature might be out of scope
- the issue seems less important
- only one stakeholder mentioned it

If uncertain whether two findings should be merged,
keep them separate.

Do not:

- create final requirements
- resolve disagreements
- make architecture decisions
- choose which stakeholder is correct
- decide final scope

The architect will perform those decisions later.
"""


def consolidate_batch(discoveries):
    """
    Consolidates a small batch while preserving
    every source discovery.
    """

    question = f"""
Analyse this small batch of SmartRail discoveries.

Every supplied source_ref MUST appear in at least one
output finding.

If an item cannot be meaningfully grouped, create a
UNIQUE_IMPORTANT_FINDING for it.

DISCOVERIES:

{json.dumps(discoveries, indent=2, ensure_ascii=False)}
"""

    return run_structured_agent(
        role=BATCH_ROLE,
        question=question,
        schema=ConsolidationReport,
    )


def merge_findings(findings, total_source_discoveries):
    """
    Performs the cross-batch/cross-stakeholder merge.
    """

    data = [
        finding.model_dump()
        for finding in findings
    ]

    question = f"""
Merge these validated intermediate SmartRail findings.

Look specifically for:

- agreement across stakeholders
- overlapping concerns
- conflicting expectations
- unclear ownership
- unresolved questions
- scope tension

Preserve every original source_ref contained in the input.

TOTAL ORIGINAL DISCOVERIES:
{total_source_discoveries}

INTERMEDIATE FINDINGS:

{json.dumps(data, indent=2, ensure_ascii=False)}
"""

    return run_structured_agent(
        role=MERGE_ROLE,
        question=question,
        schema=ConsolidationReport,
    )