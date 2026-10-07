import argparse
import json
from pathlib import Path

from agents.consolidator import (
    ConsolidatedFinding,
    consolidate_batch,
    merge_findings,
)


INPUT_DIR = Path("outputs/discovery")

OUTPUT_DIR = Path("outputs/consolidated")

BATCH_DIR = OUTPUT_DIR / "batches"

OUTPUT_DIR.mkdir(
    parents=True,
    exist_ok=True,
)

BATCH_DIR.mkdir(
    parents=True,
    exist_ok=True,
)


BATCH_SIZE = 20


def load_discoveries():

    discoveries = []

    json_files = sorted(
        INPUT_DIR.glob("*.json")
    )

    if not json_files:
        raise FileNotFoundError(
            "No discovery JSON files found."
        )

    for json_file in json_files:

        stakeholder = json_file.stem

        data = json.loads(
            json_file.read_text(
                encoding="utf-8"
            )
        )

        items = data.get(
            "discoveries",
            []
        )

        for index, item in enumerate(
            items,
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
                    "category": item.get(
                        "category"
                    ),
                    "title": item.get(
                        "title"
                    ),
                    "discovery": item.get(
                        "discovery"
                    ),
                    "source_basis": item.get(
                        "source_basis"
                    ),
                    "origin": item.get(
                        "origin"
                    ),
                    "open_question": item.get(
                        "open_question"
                    ),
                    "scope_signal": item.get(
                        "scope_signal"
                    ),
                }
            )

    return discoveries


def chunks(items, size):

    for index in range(
        0,
        len(items),
        size,
    ):
        yield items[
            index:index + size
        ]


def make_fallback(item):
    """
    Deterministic fallback.

    If AI accidentally omits an original discovery,
    Python preserves it instead of losing evidence.
    """

    workshop_categories = {
        "CONFLICT",
        "AMBIGUITY",
        "ASSUMPTION",
        "SECURITY",
        "INTEGRATION",
        "SCOPE_CANDIDATE",
    }

    workshop_required = (
        item.get("open_question") is not None
        or item.get("category")
        in workshop_categories
    )

    return ConsolidatedFinding(
        finding_type="UNIQUE_IMPORTANT_FINDING",

        title=item["title"],

        summary=item["discovery"],

        source_refs=[
            item["source_ref"]
        ],

        stakeholders=[
            item["stakeholder"]
        ],

        categories=[
            item["category"]
        ],

        unresolved_question=item.get(
            "open_question"
        ),

        workshop_required=workshop_required,

        rationale=(
            "Automatically preserved by the "
            "traceability guard because the AI "
            "consolidator did not reference this "
            "source discovery."
        ),
    )


def validate_batch(
    batch,
    findings,
):

    valid_refs = {
        item["source_ref"]
        for item in batch
    }

    used_refs = set()

    clean_findings = []

    invalid_refs = set()

    for finding in findings:

        clean_refs = []

        for ref in finding.source_refs:

            if ref in valid_refs:
                clean_refs.append(ref)
                used_refs.add(ref)

            else:
                invalid_refs.add(ref)

        if clean_refs:

            finding.source_refs = (
                clean_refs
            )

            clean_findings.append(
                finding
            )

    missing_refs = (
        valid_refs - used_refs
    )

    lookup = {
        item["source_ref"]: item
        for item in batch
    }

    # Deterministically preserve anything
    # the model forgot.
    for ref in sorted(missing_refs):

        clean_findings.append(
            make_fallback(
                lookup[ref]
            )
        )

    return (
        clean_findings,
        sorted(missing_refs),
        sorted(invalid_refs),
    )


def validate_final(
    all_discoveries,
    merged_findings,
    batch_findings,
):

    all_refs = {
        item["source_ref"]
        for item in all_discoveries
    }

    used_refs = set()

    valid_final_findings = []

    invalid_refs = set()

    for finding in merged_findings:

        clean_refs = [
            ref
            for ref in finding.source_refs
            if ref in all_refs
        ]

        invalid_refs.update(
            set(finding.source_refs)
            - all_refs
        )

        if clean_refs:

            finding.source_refs = (
                clean_refs
            )

            valid_final_findings.append(
                finding
            )

            used_refs.update(
                clean_refs
            )

    missing_refs = (
        all_refs - used_refs
    )

    #
    # If final AI merge loses evidence,
    # restore the already validated
    # intermediate finding containing it.
    #

    restored_keys = set()

    restored_count = 0

    for ref in sorted(missing_refs):

        for finding in batch_findings:

            if ref not in finding.source_refs:
                continue

            key = tuple(
                sorted(
                    finding.source_refs
                )
            )

            if key not in restored_keys:

                valid_final_findings.append(
                    finding
                )

                restored_keys.add(key)

                restored_count += 1

            break

    # Recheck final coverage
    final_refs = set()

    for finding in valid_final_findings:
        final_refs.update(
            finding.source_refs
        )

    remaining_missing = (
        all_refs - final_refs
    )

    return (
        valid_final_findings,
        sorted(remaining_missing),
        sorted(invalid_refs),
        restored_count,
    )


def create_markdown(
    total_sources,
    findings,
    remaining_missing,
):

    lines = [
        "# SmartRail Cross-Stakeholder Consolidation",
        "",
        (
            f"**Original discoveries:** "
            f"{total_sources}"
        ),
        "",
        (
            f"**Final consolidated findings:** "
            f"{len(findings)}"
        ),
        "",
        (
            f"**Uncovered discoveries:** "
            f"{len(remaining_missing)}"
        ),
        "",
    ]

    for index, finding in enumerate(
        findings,
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
            "# Traceability Validation",
            "",
        ]
    )

    if remaining_missing:

        lines.append(
            "WARNING: The following source "
            "discoveries are still uncovered:"
        )

        lines.append("")

        for ref in remaining_missing:
            lines.append(
                f"- {ref}"
            )

    else:

        lines.append(
            "PASS — All original source "
            "discoveries are represented."
        )

    return "\n".join(lines)


parser = argparse.ArgumentParser()

parser.add_argument(
    "--force",
    action="store_true",
)

args = parser.parse_args()


all_discoveries = (
    load_discoveries()
)


print("=" * 60)
print("Coverage-First Consolidation")
print("=" * 60)

print(
    f"Loaded {len(all_discoveries)} "
    f"source discoveries."
)


#
# Save source snapshot
#

source_file = (
    OUTPUT_DIR
    / "all_discoveries.json"
)

source_file.write_text(
    json.dumps(
        all_discoveries,
        indent=2,
        ensure_ascii=False,
    ),
    encoding="utf-8",
)


#
# PASS 1 — SMALL BATCHES
#

batch_findings = []

batches = list(
    chunks(
        all_discoveries,
        BATCH_SIZE,
    )
)


print()
print(
    f"Processing {len(batches)} "
    f"batches..."
)


for batch_number, batch in enumerate(
    batches,
    start=1,
):

    batch_file = (
        BATCH_DIR
        / f"batch_{batch_number:02d}.json"
    )

    print()
    print("-" * 60)

    print(
        f"Batch {batch_number}/"
        f"{len(batches)} "
        f"({len(batch)} discoveries)"
    )

    #
    # Resume support
    #

    if (
        batch_file.exists()
        and not args.force
    ):

        print(
            "Existing validated batch found. "
            "Loading without API call."
        )

        saved_data = json.loads(
            batch_file.read_text(
                encoding="utf-8"
            )
        )

        findings = [
            ConsolidatedFinding(
                **item
            )
            for item
            in saved_data["findings"]
        ]

        batch_findings.extend(
            findings
        )

        continue

    report = consolidate_batch(
        batch
    )

    (
        validated_findings,
        missing,
        invalid,
    ) = validate_batch(
        batch,
        report.consolidated_findings,
    )

    print(
        f"AI findings: "
        f"{len(report.consolidated_findings)}"
    )

    print(
        f"AI omitted: "
        f"{len(missing)}"
    )

    print(
        f"Invalid refs: "
        f"{len(invalid)}"
    )

    if missing:

        print(
            "Traceability guard restored "
            f"{len(missing)} discoveries."
        )

    batch_file.write_text(
        json.dumps(
            {
                "batch_number":
                    batch_number,

                "input_count":
                    len(batch),

                "ai_missing_refs":
                    missing,

                "invalid_refs":
                    invalid,

                "findings": [
                    item.model_dump()
                    for item
                    in validated_findings
                ],
            },
            indent=2,
            ensure_ascii=False,
        ),
        encoding="utf-8",
    )

    batch_findings.extend(
        validated_findings
    )


print()
print("=" * 60)
print("Batch consolidation complete")
print("=" * 60)

print(
    f"Validated intermediate findings: "
    f"{len(batch_findings)}"
)


#
# PASS 2 — CROSS-STAKEHOLDER MERGE
#

print()
print("=" * 60)
print("Cross-Stakeholder Merge")
print("=" * 60)


merged_report = merge_findings(
    findings=batch_findings,
    total_source_discoveries=len(
        all_discoveries
    ),
)


(
    final_findings,
    remaining_missing,
    invalid_final_refs,
    restored_count,
) = validate_final(
    all_discoveries,
    merged_report.consolidated_findings,
    batch_findings,
)


print()
print(
    f"AI merged findings: "
    f"{len(merged_report.consolidated_findings)}"
)

print(
    f"Intermediate findings restored "
    f"by traceability guard: "
    f"{restored_count}"
)

print(
    f"Invalid final refs: "
    f"{len(invalid_final_refs)}"
)

print(
    f"Final uncovered discoveries: "
    f"{len(remaining_missing)}"
)


#
# SAVE FINAL OUTPUT
#

final_json = (
    OUTPUT_DIR
    / "consolidation_v2.json"
)

final_md = (
    OUTPUT_DIR
    / "consolidation_v2.md"
)


final_json.write_text(
    json.dumps(
        {
            "total_source_discoveries":
                len(all_discoveries),

            "final_findings": [
                finding.model_dump()
                for finding
                in final_findings
            ],

            "traceability": {
                "uncovered_refs":
                    remaining_missing,

                "invalid_refs":
                    invalid_final_refs,

                "restored_intermediate_findings":
                    restored_count,
            },
        },
        indent=2,
        ensure_ascii=False,
    ),
    encoding="utf-8",
)


final_md.write_text(
    create_markdown(
        total_sources=len(
            all_discoveries
        ),
        findings=final_findings,
        remaining_missing=(
            remaining_missing
        ),
    ),
    encoding="utf-8",
)


print()
print(
    f"Saved -> {final_json}"
)

print(
    f"Saved -> {final_md}"
)

print()
print("=" * 60)

if remaining_missing:

    print(
        "FAILED TRACEABILITY CHECK"
    )

else:

    print(
        "PASS — 100% source coverage"
    )

print("=" * 60)