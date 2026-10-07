import json
from pathlib import Path

from agents.workshop_planner import (
    plan_workshops,
)


INPUT_FILE = Path(
    "outputs/consolidated/consolidation_v2.json"
)

OUTPUT_DIR = Path(
    "outputs/workshops"
)

OUTPUT_DIR.mkdir(
    parents=True,
    exist_ok=True,
)


data = json.loads(
    INPUT_FILE.read_text(
        encoding="utf-8"
    )
)


all_findings = data[
    "final_findings"
]


#
# Give every finding a stable CF ID
#

workshop_candidates = []

for index, finding in enumerate(
    all_findings,
    start=1,
):

    finding_ref = f"CF-{index:03d}"

    if not finding.get(
        "workshop_required",
        False,
    ):
        continue

    workshop_candidates.append(
        {
            "finding_ref": finding_ref,
            "finding_type":
                finding["finding_type"],

            "title":
                finding["title"],

            "summary":
                finding["summary"],

            "stakeholders":
                finding["stakeholders"],

            "categories":
                finding["categories"],

            "source_refs":
                finding["source_refs"],

            "unresolved_question":
                finding.get(
                    "unresolved_question"
                ),
        }
    )


print("=" * 60)
print("Workshop Planning")
print("=" * 60)

print(
    f"Workshop-required findings: "
    f"{len(workshop_candidates)}"
)


#
# Save exact planner input
#

input_file = (
    OUTPUT_DIR
    / "workshop_input.json"
)

input_file.write_text(
    json.dumps(
        workshop_candidates,
        indent=2,
        ensure_ascii=False,
    ),
    encoding="utf-8",
)


#
# Ask planner
#

report = plan_workshops(
    json.dumps(
        workshop_candidates,
        indent=2,
        ensure_ascii=False,
    )
)


#
# Validate finding coverage
#

valid_refs = {
    item["finding_ref"]
    for item in workshop_candidates
}

used_refs = set()

invalid_refs = set()

workshops = []


for index, workshop in enumerate(
    report.workshops,
    start=1,
):

    clean_refs = []

    for ref in workshop.finding_refs:

        if ref in valid_refs:

            clean_refs.append(ref)
            used_refs.add(ref)

        else:

            invalid_refs.add(ref)

    if not clean_refs:
        continue

    workshops.append(
        {
            "workshop_id":
                f"WS-{index:03d}",

            "title":
                workshop.title,

            "issue_summary":
                workshop.issue_summary,

            "finding_refs":
                clean_refs,

            "participants":
                workshop.participants,

            "objective":
                workshop.objective,

            "discussion_questions":
                workshop.discussion_questions,
        }
    )


missing_refs = (
    valid_refs - used_refs
)


#
# Deterministic fallback if planner forgot
# anything.
#

lookup = {
    item["finding_ref"]: item
    for item in workshop_candidates
}


for ref in sorted(missing_refs):

    finding = lookup[ref]

    participants = list(
        finding["stakeholders"]
    )

    allowed = {
        "passenger",
        "driver",
        "station_manager",
        "railway_controller",
        "catering_provider",
        "customer_service",
        "product_owner",
    }

    participants = [
        participant
        for participant in participants
        if participant in allowed
    ]


    #
    # A workshop needs at least two views.
    #

    if (
        len(participants) < 2
        and "product_owner"
        not in participants
    ):
        participants.append(
            "product_owner"
        )


    workshop_id = (
        f"WS-{len(workshops) + 1:03d}"
    )


    workshops.append(
        {
            "workshop_id":
                workshop_id,

            "title":
                finding["title"],

            "issue_summary":
                finding["summary"],

            "finding_refs":
                [ref],

            "participants":
                participants,

            "objective":
                (
                    "Clarify this unresolved "
                    "SmartRail discovery."
                ),

            "discussion_questions":
                [
                    finding.get(
                        "unresolved_question"
                    )
                    or
                    (
                        "What decision is required "
                        "for this issue?"
                    )
                ],
        }
    )


#
# Save plan
#

plan_data = {
    "workshop_candidate_count":
        len(workshop_candidates),

    "workshop_count":
        len(workshops),

    "invalid_refs":
        sorted(invalid_refs),

    "planner_omitted_refs":
        sorted(missing_refs),

    "workshops":
        workshops,
}


plan_file = (
    OUTPUT_DIR
    / "workshop_plan.json"
)

plan_file.write_text(
    json.dumps(
        plan_data,
        indent=2,
        ensure_ascii=False,
    ),
    encoding="utf-8",
)


#
# Human-readable Markdown
#

lines = [
    "# SmartRail Stakeholder Workshop Plan",
    "",
    (
        f"Workshop candidates: "
        f"{len(workshop_candidates)}"
    ),
    "",
    (
        f"Planned workshops: "
        f"{len(workshops)}"
    ),
    "",
]


for workshop in workshops:

    lines.extend(
        [
            (
                f"## {workshop['workshop_id']} "
                f"— {workshop['title']}"
            ),
            "",
            (
                f"**Issue:** "
                f"{workshop['issue_summary']}"
            ),
            "",
            (
                f"**Findings:** "
                f"{', '.join(workshop['finding_refs'])}"
            ),
            "",
            (
                f"**Participants:** "
                f"{', '.join(workshop['participants'])}"
            ),
            "",
            (
                f"**Objective:** "
                f"{workshop['objective']}"
            ),
            "",
            "**Discussion questions:**",
            "",
        ]
    )

    for question in workshop[
        "discussion_questions"
    ]:
        lines.append(
            f"- {question}"
        )

    lines.append("")


markdown_file = (
    OUTPUT_DIR
    / "workshop_plan.md"
)

markdown_file.write_text(
    "\n".join(lines),
    encoding="utf-8",
)


print(
    f"Planned workshops: "
    f"{len(workshops)}"
)

print(
    f"Planner omitted findings: "
    f"{len(missing_refs)}"
)

print(
    f"Invalid finding refs: "
    f"{len(invalid_refs)}"
)

print()

print(
    f"Saved -> {input_file}"
)

print(
    f"Saved -> {plan_file}"
)

print(
    f"Saved -> {markdown_file}"
)