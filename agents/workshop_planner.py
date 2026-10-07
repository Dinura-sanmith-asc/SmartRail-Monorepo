from typing import Literal

from pydantic import BaseModel, Field

from agents.base import run_structured_agent


Stakeholder = Literal[
    "passenger",
    "driver",
    "station_manager",
    "railway_controller",
    "catering_provider",
    "customer_service",
    "product_owner",
]


class WorkshopTopic(BaseModel):

    title: str

    issue_summary: str

    finding_refs: list[str] = Field(
        min_length=1
    )

    participants: list[Stakeholder] = Field(
        min_length=2
    )

    objective: str

    discussion_questions: list[str] = Field(
        min_length=1
    )


class WorkshopPlan(BaseModel):

    workshops: list[WorkshopTopic]


ROLE = """
You are the SmartRail Stakeholder Workshop Planner.

You receive consolidated requirement-discovery findings
that were marked as requiring stakeholder discussion.

Your responsibility is to group related unresolved findings
into focused stakeholder workshops.

A workshop should represent ONE meaningful decision area,
for example:

- operational authority
- journey state ownership
- disruption communication
- platform changes
- passenger recovery
- catering during disruption
- scope boundaries

Rules:

1. Do not resolve any issue.

2. Do not create final requirements.

3. Every supplied finding_ref must appear in at least one
   workshop.

4. Never invent finding_ref values.

5. Group findings only when they concern the same underlying
   business decision.

6. Choose only stakeholders genuinely relevant to the issue.

7. Prefer 2-4 participants per workshop.

8. Avoid inviting every stakeholder to every workshop.

9. The Product Owner may participate when scope, priorities,
   ownership, or business boundaries need clarification.

10. Discussion questions should expose disagreements,
    ownership questions, assumptions, edge cases, and
    decision points.

11. Aim for a manageable number of focused workshops rather
    than one huge discussion.

The architect will make final decisions after the workshops.
"""


def plan_workshops(findings):

    return run_structured_agent(
        role=ROLE,
        question=f"""
Create a stakeholder workshop plan from these unresolved
SmartRail findings:

{findings}

Make sure every finding_ref appears in at least one workshop.
""",
        schema=WorkshopPlan,
    )