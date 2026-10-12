
import json
import re
from pathlib import Path

from agents.baseline_extractor import extract_baseline


INPUT_FILE = Path("inputs/assessment.md")

OUTPUT_DIR = Path("outputs/requirements")
BATCH_DIR = OUTPUT_DIR / "baseline_batches"

BATCH_DIR.mkdir(parents=True, exist_ok=True)

BATCH_SIZE = 3


def parse_sections(markdown):
    pattern = re.compile(
        r"^#\s+(\d+)\.\s+(.+)$",
        re.MULTILINE,
    )

    matches = list(pattern.finditer(markdown))

    sections = []

    for index, match in enumerate(matches):
        end = (
            matches[index + 1].start()
            if index + 1 < len(matches)
            else len(markdown)
        )

        sections.append({
            "source_ref": f"BRIEF-{match.group(1)}",
            "section_number": int(match.group(1)),
            "heading": match.group(2).strip(),
            "content": markdown[match.end():end].strip(),
        })

    return sections


def chunks(items, size):
    for index in range(0, len(items), size):
        yield items[index:index + size]


markdown = INPUT_FILE.read_text(encoding="utf-8")
sections = parse_sections(markdown)

if not sections:
    raise ValueError(
        "No numbered assessment sections found. "
        "Check inputs/assessment.md."
    )

print("=" * 60)
print("SmartRail Baseline Extraction")
print("=" * 60)
print(f"Found {len(sections)} assessment sections.")

valid_refs = {
    section["source_ref"]
    for section in sections
}

all_items = []

batches = list(chunks(sections, BATCH_SIZE))

for number, batch in enumerate(batches, start=1):
    batch_path = BATCH_DIR / f"batch_{number:02d}.json"

    print(
        f"\nBatch {number}/{len(batches)} "
        f"— sections: "
        f"{', '.join(x['source_ref'] for x in batch)}"
    )

    if batch_path.exists():
        print("Using saved batch; no API call.")
        items = json.loads(
            batch_path.read_text(encoding="utf-8")
        )
    else:
        report = extract_baseline(batch)

        items = [
            item.model_dump()
            for item in report.items
        ]

        permitted = {
            section["source_ref"]
            for section in batch
        }

        invalid = [
            item["source_ref"]
            for item in items
            if item["source_ref"] not in permitted
        ]

        if invalid:
            raise ValueError(
                f"Invalid source references in batch "
                f"{number}: {invalid}"
            )

        batch_path.write_text(
            json.dumps(
                items, indent=2, ensure_ascii=False
            ),
            encoding="utf-8",
        )

    all_items.extend(items)
    print(f"Extracted {len(items)} items.")


# Stable IDs for later traceability
for index, item in enumerate(all_items, start=1):
    item["candidate_id"] = f"BASE-{index:03d}"


invalid_refs = sorted({
    item["source_ref"]
    for item in all_items
    if item["source_ref"] not in valid_refs
})

if invalid_refs:
    raise ValueError(f"Invalid refs: {invalid_refs}")


# Section-level coverage audit
covered = {
    item["source_ref"]
    for item in all_items
}

uncovered_sections = [
    section["source_ref"]
    for section in sections
    if section["source_ref"] not in covered
]

output_file = (
    OUTPUT_DIR / "baseline_candidates.json"
)

output_file.write_text(
    json.dumps(
        {
            "total_sections": len(sections),
            "candidate_count": len(all_items),
            "uncovered_sections": uncovered_sections,
            "items": all_items,
        },
        indent=2,
        ensure_ascii=False,
    ),
    encoding="utf-8",
)


# Human-readable output
lines = [
    "# SmartRail Baseline Requirement Candidates",
    "",
    f"**Source sections:** {len(sections)}",
    f"**Extracted items:** {len(all_items)}",
    "",
]

for item in all_items:
    lines.extend([
        f"## {item['candidate_id']} — {item['title']}",
        "",
        f"**Source:** {item['source_ref']}",
        f"**Classification:** {item['classification']}",
        f"**Strength:** {item['source_strength']}",
        "",
        item["statement"],
        "",
        f"**Evidence:** {item['evidence_excerpt']}",
        "",
        (
            "**Clarification:** "
            + str(item.get("clarification_question") or "None")
        ),
        "",
    ])

lines.extend([
    "## Section Coverage Audit",
    "",
    (
        "Uncovered sections: "
        + (
            ", ".join(uncovered_sections)
            if uncovered_sections
            else "None"
        )
    ),
])

md_file = (
    OUTPUT_DIR / "baseline_candidates.md"
)

md_file.write_text(
    "\n".join(lines),
    encoding="utf-8",
)

print()
print("=" * 60)
print(f"Extracted candidates: {len(all_items)}")
print(f"Uncovered sections: {uncovered_sections}")
print(f"Saved -> {output_file}")
print(f"Saved -> {md_file}")
print("=" * 60)
