import argparse
import json
from pathlib import Path

from agents.registry import ask_agent
from agents.workshop_moderator import moderate


WORKSHOP_DIR = Path(
    "outputs/workshops"
)

PLAN_FILE = (
    WORKSHOP_DIR
    / "workshop_plan.json"
)

INPUT_FILE = (
    WORKSHOP_DIR
    / "workshop_input.json"
)


parser = argparse.ArgumentParser()

parser.add_argument(
    "--id",
    required=True,
    help="Workshop ID, e.g. WS-001",
)

args = parser.parse_args()


#
# Load workshop plan
#

plan = json.loads(
    PLAN_FILE.read_text(
        encoding="utf-8"
    )
)

finding_data = json.loads(
    INPUT_FILE.read_text(
        encoding="utf-8"
    )
)


workshop = next(
    (
        item
        for item in plan["workshops"]
        if item["workshop_id"]
        == args.id
    ),
    None,
)


if workshop is None:
    raise ValueError(
        f"Workshop {args.id} not found."
    )


finding_lookup = {
    item["finding_ref"]: item
    for item in finding_data
}


relevant_findings = [
    finding_lookup[ref]
    for ref
    in workshop["finding_refs"]
]


#
# Workshop-specific output folder
#

output_dir = (
    WORKSHOP_DIR
    / workshop["workshop_id"]
)

output_dir.mkdir(
    parents=True,
    exist_ok=True,
)


print("=" * 60)

print(
    f"{workshop['workshop_id']} "
    f"— {workshop['title']}"
)

print("=" * 60)

print(
    "Participants: "
    + ", ".join(
        workshop["participants"]
    )
)


#
# -----------------------
# ROUND 1
# Independent positions
# -----------------------
#

round1 = {}


for participant in workshop[
    "participants"
]:

    response_file = (
        output_dir
        / f"round1_{participant}.txt"
    )

    print()
    print(
        f"Round 1 -> {participant}"
    )

    if response_file.exists():

        print(
            "Existing response found. "
            "Skipping API call."
        )

        response = (
            response_file.read_text(
                encoding="utf-8"
            )
        )

    else:

        prompt = f"""
You are participating in this SmartRail stakeholder workshop.

WORKSHOP:
{workshop['title']}

ISSUE:
{workshop['issue_summary']}

OBJECTIVE:
{workshop['objective']}

RELEVANT DISCOVERY FINDINGS:

{json.dumps(
    relevant_findings,
    indent=2,
    ensure_ascii=False
)}

DISCUSSION QUESTIONS:

{json.dumps(
    workshop['discussion_questions'],
    indent=2,
    ensure_ascii=False
)}

This is Round 1.

Give your position independently.

Explain:

1. What matters from your stakeholder perspective.
2. What responsibility you believe your role has.
3. What responsibility your role should NOT have.
4. What risks or failure cases concern you.
5. What decision you believe needs clarification.
6. Any assumption you disagree with.

Do not pretend to speak for other stakeholders.
Do not design the technical architecture.
Be concise but specific.
"""

        response = ask_agent(
            participant,
            prompt,
        )

        response_file.write_text(
            response,
            encoding="utf-8",
        )

    round1[participant] = response


#
# -----------------------
# ROUND 2
# Challenge round
# -----------------------
#

round2 = {}


for participant in workshop[
    "participants"
]:

    response_file = (
        output_dir
        / f"round2_{participant}.txt"
    )

    print()
    print(
        f"Round 2 -> {participant}"
    )

    if response_file.exists():

        print(
            "Existing response found. "
            "Skipping API call."
        )

        response = (
            response_file.read_text(
                encoding="utf-8"
            )
        )

    else:

        other_positions = {
            name: position
            for name, position
            in round1.items()
            if name != participant
        }


        prompt = f"""
You are now in Round 2 of the SmartRail stakeholder workshop.

WORKSHOP:
{workshop['title']}

Your original position was:

-------------------------
{round1[participant]}
-------------------------

Other stakeholders said:

-------------------------
{json.dumps(
    other_positions,
    indent=2,
    ensure_ascii=False
)}
-------------------------

Respond to the other stakeholders.

Your job is NOT simply to agree.

Identify:

1. Points you agree with.
2. Points you challenge.
3. Responsibilities that appear unclear.
4. Risks another stakeholder may have overlooked.
5. Any proposal that would create problems for your role.
6. Questions that still require an architect or business
   decision.

Stay within your stakeholder perspective.

Do not make architecture decisions.
"""

        response = ask_agent(
            participant,
            prompt,
        )

        response_file.write_text(
            response,
            encoding="utf-8",
        )

    round2[participant] = response


#
# -----------------------
# MODERATOR
# -----------------------
#

summary_json = (
    output_dir
    / "summary.json"
)

summary_md = (
    output_dir
    / "summary.md"
)


print()
print("Moderator -> summarising discussion")


if summary_json.exists():

    print(
        "Existing moderator summary found."
    )

    summary_data = json.loads(
        summary_json.read_text(
            encoding="utf-8"
        )
    )

else:

    moderator_input = f"""
SMART RAIL STAKEHOLDER WORKSHOP

WORKSHOP ID:
{workshop['workshop_id']}

TOPIC:
{workshop['title']}

ISSUE:
{workshop['issue_summary']}

FINDINGS:
{json.dumps(
    relevant_findings,
    indent=2,
    ensure_ascii=False
)}

ROUND 1 — INDEPENDENT POSITIONS:
{json.dumps(
    round1,
    indent=2,
    ensure_ascii=False
)}

ROUND 2 — CHALLENGES:
{json.dumps(
    round2,
    indent=2,
    ensure_ascii=False
)}

Summarise the workshop neutrally.

Do not make the final decision.
"""

    summary = moderate(
        moderator_input
    )

    summary_data = (
        summary.model_dump()
    )

    summary_json.write_text(
        json.dumps(
            summary_data,
            indent=2,
            ensure_ascii=False,
        ),
        encoding="utf-8",
    )


#
# Human readable summary
#

lines = [
    (
        f"# {workshop['workshop_id']} "
        f"— {workshop['title']}"
    ),
    "",
    "## Participants",
    "",
]

for participant in workshop[
    "participants"
]:
    lines.append(
        f"- {participant}"
    )


def add_section(
    title,
    values,
):

    lines.extend(
        [
            "",
            f"## {title}",
            "",
        ]
    )

    if not values:
        lines.append(
            "None identified."
        )
        return

    for value in values:
        lines.append(
            f"- {value}"
        )


add_section(
    "Agreements",
    summary_data["agreements"],
)

add_section(
    "Disagreements",
    summary_data["disagreements"],
)

add_section(
    "Ownership Questions",
    summary_data[
        "ownership_questions"
    ],
)

add_section(
    "Scope Questions",
    summary_data[
        "scope_questions"
    ],
)

add_section(
    "Assumptions to Validate",
    summary_data[
        "assumptions_to_validate"
    ],
)


lines.extend(
    [
        "",
        "## Decision Points",
        "",
    ]
)


for index, decision in enumerate(
    summary_data[
        "decision_points"
    ],
    start=1,
):

    lines.append(
        f"### Decision {index}"
    )

    lines.append("")

    lines.append(
        decision["question"]
    )

    lines.append("")

    lines.append(
        "**Options:**"
    )

    lines.append("")

    for option in decision["options"]:
        lines.append(
            f"- {option}"
        )

    lines.append("")

    lines.append(
        "**Affected findings:** "
        + ", ".join(
            decision[
                "affected_findings"
            ]
        )
    )

    lines.append("")


add_section(
    "Unresolved Questions",
    summary_data[
        "unresolved_questions"
    ],
)


summary_md.write_text(
    "\n".join(lines),
    encoding="utf-8",
)


print()
print(f"Saved -> {summary_json}")
print(f"Saved -> {summary_md}")

print()
print("=" * 60)
print("Workshop complete.")
print("=" * 60)