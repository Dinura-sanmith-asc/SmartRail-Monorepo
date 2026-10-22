
import argparse
import csv
import json
from pathlib import Path

from agents.base import run_structured_agent
from agents.review_schema import ReviewReport

from agents import (
    security_engineer,
    qa_engineer,
    accessibility_specialist,
)


INPUT_DIR = Path("inputs")
OUTPUT_DIR = Path("outputs/reviews")
OUTPUT_DIR.mkdir(parents=True, exist_ok=True)


ROLES = {
    "security": security_engineer.ROLE,
    "qa": qa_engineer.ROLE,
    "accessibility": accessibility_specialist.ROLE,
}


def load_decisions():
    path = INPUT_DIR / "architect_decisions.csv"

    with path.open(
        "r", encoding="utf-8-sig", newline=""
    ) as f:
        rows = list(csv.DictReader(f))

    if len(rows) != 79:
        raise ValueError(
            f"Expected 79 decisions, found {len(rows)}"
        )

    return [
        {
            "id": row["Decision ID"],
            "workshop": row["Workshop"],
            "topic": row["Topic"],
            "original_proposal": row["Proposed Decision"],
            "status": row["Status"],
            "architect_reason": row["Your Reason"],
            "affected_findings": row["Affected Findings"],
        }
        for row in rows
    ]


def save_markdown(name, report):
    lines = [
        f"# SmartRail {name.title()} Review",
        "",
        f"**Findings:** {len(report.findings)}",
        "",
    ]

    for i, item in enumerate(report.findings, 1):
        lines.extend([
            f"## {name.upper()}-{i:03d} — {item.title}",
            "",
            f"**Category:** {item.category}",
            f"**Severity:** {item.severity}",
            f"**Area:** {item.application_area}",
            "",
            f"**Finding:** {item.finding}",
            "",
            f"**Candidate requirement:** "
            f"{item.proposed_requirement}",
            "",
            f"**Sources:** {', '.join(item.evidence_refs)}",
            "",
            f"**Question:** {item.validation_question}",
            "",
            f"**Verification:** {item.verification_idea}",
            "",
            "**Architect status:** PENDING REVIEW",
            "",
        ])

    lines.append("## Overall Observations")
    lines.append("")

    for observation in report.overall_observations:
        lines.append(f"- {observation}")

    return "\n".join(lines)


parser = argparse.ArgumentParser()
parser.add_argument(
    "--review",
    choices=list(ROLES),
    required=True,
)
parser.add_argument("--force", action="store_true")
args = parser.parse_args()

json_path = OUTPUT_DIR / f"{args.review}.json"
md_path = OUTPUT_DIR / f"{args.review}.md"

if json_path.exists() and not args.force:
    print("Existing review found. Skipping API call.")
    raise SystemExit

brief = (
    INPUT_DIR / "assessment.md"
).read_text(encoding="utf-8")

decisions = load_decisions()

context = f"""
ORIGINAL SMART RAIL ASSESSMENT:

{brief}

ARCHITECT'S EVALUATED DECISIONS:

{json.dumps(decisions, ensure_ascii=False)}

REVIEW INSTRUCTIONS:

1. The assessment is the baseline business brief.
2. The decisions represent the architect's evaluated choices.
3. ACCEPTED: selected direction.
4. MODIFIED: read the original proposal AND architect_reason;
   do not treat the original proposal as fully accepted.
5. DEFERRED: out of current MVP, not necessarily irrelevant.
6. REJECTED: do not reintroduce without identifying a
   genuine mandatory requirement or material risk.
7. ASSUMPTION: not yet verified.
8. Do not treat simulated stakeholder statements as
   confirmed railway policy.
9. Identify missing requirements, risks, ambiguities
   and testable scenarios.
10. All findings remain candidates pending architect review.
11. Cover all relevant SmartRail applications.
12. Do not invent evidence references.
"""

print(f"Starting {args.review} review...")
print(f"Loaded {len(decisions)} architect decisions.")

report = run_structured_agent(
    role=ROLES[args.review],
    question=context,
    schema=ReviewReport,
)

data = report.model_dump()

valid_refs = {
    row["id"] for row in decisions
} | {"BRIEF"}

invalid_refs = sorted({
    ref
    for item in report.findings
    for ref in item.evidence_refs
    if ref not in valid_refs
})

if invalid_refs:
    print("WARNING: Invalid evidence refs:", invalid_refs)

json_path.write_text(
    json.dumps(data, indent=2, ensure_ascii=False),
    encoding="utf-8",
)

md_path.write_text(
    save_markdown(args.review, report),
    encoding="utf-8",
)

print(f"Findings: {len(report.findings)}")
print(f"Saved: {json_path}")
print(f"Saved: {md_path}")
print("Review complete.")
