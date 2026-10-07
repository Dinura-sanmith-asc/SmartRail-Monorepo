import argparse
import json
from pathlib import Path

from agents.consolidator import consolidate


INPUT_DIR = Path("outputs/discovery")
OUTPUT_DIR = Path("outputs/consolidated")

OUTPUT_DIR.mkdir(
    parents=True,
    exist_ok=True,
)


def load_discoveries():

    discoveries = []

    json_files = sorted(INPUT_DIR.glob("*.json"))

    if not json_files:
        raise FileNotFoundError(
            "No JSON discovery files found."
        )

    for json_file in json_files:

        stakeholder = json_file.stem

        data = json.loads(
            json_file.read_text(
                encoding="utf-8"
            )
        )

        stakeholder_discoveries = data.get(
            "discoveries",
            []
        )

        for index, item in enumerate(
            stakeholder_discoveries,
            start=1,
        ):

            source_ref = (
                f"{stakeholder.upper()}-"
                f"{index:03d}"
            )

            discoveries.append(
                {
                    "source_ref": source_ref,
                    "stakeholder": stakeholder,
                    "category": item.get("category"),
                    "title": item.get("title"),
                    "discovery": item.get("discovery"),
                    "source_basis": item.get(
                        "source_basis"
                    ),
                    "origin": item.get("origin"),
                    "open_question": item.get(
                        "open_question"
                    ),
                    "scope_signal": item.get(
                        "scope_signal"
                    ),
                }
            )

    return discoveries


def create_markdown(
    report,
    uncovered_refs,
):

    lines = [
        "# SmartRail Cross-Stakeholder Consolidation",
        "",
        (
            f"**Source discoveries:** "
            f"{report.total_source_discoveries}"
        ),
        "",
        (
            f"**Consolidated findings:** "
            f"{len(report.consolidated_findings)}"
        ),
        "",
    ]

    for index, finding in enumerate(
        report.consolidated_findings,
        start=1,
    ):

        lines.extend(
            [
                (
                    f"## CF-{index:03d} — "
                    f"{finding.title}"
                ),
                "",
                (
                    f"- **Type:** "
                    f"{finding.finding_type}"
                ),
                (
                    f"- **Stakeholders:** "
                    f"{', '.join(finding.stakeholders)}"
                ),
                (
                    f"- **Categories:** "
                    f"{', '.join(finding.categories)}"
                ),
                (
                    f"- **Source refs:** "
                    f"{', '.join(finding.source_refs)}"
                ),
                (
                    f"- **Summary:** "
                    f"{finding.summary}"
                ),
                (
                    f"- **Workshop required:** "
                    f"{finding.workshop_required}"
                ),
                (
                    f"- **Unresolved question:** "
                    f"{finding.unresolved_question or 'None'}"
                ),
                (
                    f"- **Rationale:** "
                    f"{finding.rationale}"
                ),
                "",
            ]
        )

    lines.extend(
        [
            "# Cross-Cutting Observations",
            "",
        ]
    )

    for observation in report.cross_cutting_observations:
        lines.append(
            f"- {observation}"
        )

    lines.extend(
        [
            "",
            "# Traceability Check",
            "",
        ]
    )

    if uncovered_refs:

        lines.append(
            "The following source discoveries were "
            "not referenced by the consolidator:"
        )

        lines.append("")

        for ref in uncovered_refs:
            lines.append(f"- {ref}")

    else:
        lines.append(
            "All source discoveries were referenced "
            "by at least one consolidated finding."
        )

    return "\n".join(lines)


# -----------------------
# COMMAND LINE ARGUMENTS
# -----------------------

parser = argparse.ArgumentParser()

parser.add_argument(
    "--force",
    action="store_true",
    help="Run again even if output already exists.",
)

args = parser.parse_args()


# -----------------------
# OUTPUT FILES
# -----------------------

json_output = OUTPUT_DIR / "consolidation.json"
markdown_output = OUTPUT_DIR / "consolidation.md"
source_output = OUTPUT_DIR / "all_discoveries.json"


# -----------------------
# SKIP EXISTING OUTPUT
# -----------------------

if (
    json_output.exists()
    and markdown_output.exists()
    and not args.force
):
    print(
        "Consolidation already exists."
    )
    print(
        "Use --force if you intentionally "
        "want to run it again."
    )

    raise SystemExit


# -----------------------
# LOAD DISCOVERIES
# -----------------------

discoveries = load_discoveries()


print("=" * 60)
print("Cross-Stakeholder Consolidation")
print("=" * 60)

print(
    f"Loaded {len(discoveries)} "
    f"source discoveries."
)


# Save exactly what we send to the AI.
source_output.write_text(
    json.dumps(
        discoveries,
        indent=2,
        ensure_ascii=False,
    ),
    encoding="utf-8",
)


# -----------------------
# RUN CONSOLIDATION
# -----------------------

report = consolidate(discoveries)


# -----------------------
# TRACEABILITY VALIDATION
# -----------------------

valid_refs = {
    item["source_ref"]
    for item in discoveries
}

used_refs = set()
invalid_refs = set()


for finding in report.consolidated_findings:

    for ref in finding.source_refs:

        if ref in valid_refs:
            used_refs.add(ref)

        else:
            invalid_refs.add(ref)


uncovered_refs = sorted(
    valid_refs - used_refs
)


if invalid_refs:

    print()
    print(
        "WARNING: AI returned invalid "
        "source references:"
    )

    for ref in sorted(invalid_refs):
        print(f"  - {ref}")


if uncovered_refs:

    print()
    print(
        f"WARNING: {len(uncovered_refs)} "
        f"source discoveries were not referenced."
    )


# -----------------------
# SAVE JSON
# -----------------------

json_output.write_text(
    json.dumps(
        report.model_dump(),
        indent=2,
        ensure_ascii=False,
    ),
    encoding="utf-8",
)


# -----------------------
# SAVE MARKDOWN
# -----------------------

markdown_output.write_text(
    create_markdown(
        report,
        uncovered_refs,
    ),
    encoding="utf-8",
)


print()
print(
    f"Created "
    f"{len(report.consolidated_findings)} "
    f"consolidated findings."
)

print(
    f"Workshop candidates: "
    f"{sum(
        1
        for finding
        in report.consolidated_findings
        if finding.workshop_required
    )}"
)

print(
    f"Uncovered source discoveries: "
    f"{len(uncovered_refs)}"
)

print()
print(f"Saved -> {source_output}")
print(f"Saved -> {json_output}")
print(f"Saved -> {markdown_output}")

print()
print("=" * 60)
print("Consolidation complete.")
print("=" * 60)