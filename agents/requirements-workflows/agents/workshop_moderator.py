from pydantic import BaseModel

from agents.base import run_structured_agent


class DecisionPoint(BaseModel):

    question: str

    options: list[str]

    affected_findings: list[str]


class WorkshopSummary(BaseModel):

    agreements: list[str]

    disagreements: list[str]

    ownership_questions: list[str]

    scope_questions: list[str]

    assumptions_to_validate: list[str]

    decision_points: list[DecisionPoint]

    unresolved_questions: list[str]


ROLE = """
You are the neutral SmartRail Requirements Workshop Moderator.

You receive:

- the workshop issue
- relevant consolidated findings
- independent stakeholder positions
- stakeholder challenge responses

Your responsibility is to summarize the discussion.

Do NOT make the final decision.

Do NOT choose which stakeholder is correct.

Do NOT create final requirements.

Identify:

- genuine agreements
- genuine disagreements
- responsibility or ownership questions
- scope questions
- assumptions requiring validation
- explicit decision points
- unresolved questions

When stakeholders disagree, preserve both perspectives.

The Software Architect will make the final decisions.
"""


def moderate(workshop_content):

    return run_structured_agent(
        role=ROLE,
        question=workshop_content,
        schema=WorkshopSummary,
    )