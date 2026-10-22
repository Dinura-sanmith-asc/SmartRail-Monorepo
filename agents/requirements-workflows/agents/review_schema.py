
from typing import Literal
from pydantic import BaseModel, Field


class ReviewFinding(BaseModel):
    title: str

    category: Literal[
        "MISSING_REQUIREMENT",
        "SECURITY_RISK",
        "QA_GAP",
        "ACCESSIBILITY_GAP",
        "CONFLICT",
        "AMBIGUITY",
        "ASSUMPTION_TO_VALIDATE",
    ]

    severity: Literal[
        "CRITICAL", "HIGH", "MEDIUM", "LOW"
    ]

    application_area: str
    finding: str
    proposed_requirement: str
    evidence_refs: list[str] = Field(default_factory=list)
    validation_question: str
    verification_idea: str


class ReviewReport(BaseModel):
    reviewer: str
    findings: list[ReviewFinding]
    overall_observations: list[str]
