import json
import time
from pathlib import Path

from agents.discovery_analyst import analyze


INPUT_DIR = Path("outputs/individual")
OUTPUT_DIR = Path("outputs/discovery")

OUTPUT_DIR.mkdir(
    parents=True,
    exist_ok=True,
)


def create_markdown(report):
    stakeholder_name = report.stakeholder.replace(
        "_",
        " "
    ).title()

    lines = [
        f"# {stakeholder_name} Discovery Analysis",
        "",
    ]

    for index, item in enumerate(
        report.discoveries,
        start=1,
    ):
        lines.extend(
            [
                f"## {index}. {item.title}",
                "",
                f"- **Category:** {item.category}",
                f"- **Origin:** {item.origin}",
                f"- **Discovery:** {item.discovery}",
                f"- **Source basis:** {item.source_basis}",
                f"- **Scope signal:** {item.scope_signal}",
                (
                    f"- **Open question:** "
                    f"{item.open_question or 'None'}"
                ),
                "",
            ]
        )

    return "\n".join(lines)


files = sorted(INPUT_DIR.glob("*.md"))

if not files:
    raise FileNotFoundError(
        "No stakeholder interview files found in "
        "outputs/individual/"
    )


for input_file in files:

    stakeholder = input_file.stem

    print()
    print("=" * 60)
    print(f"Analysing: {stakeholder}")
    print("=" * 60)

    interview_text = input_file.read_text(
        encoding="utf-8"
    )

    report = analyze(
        stakeholder=stakeholder,
        interview=interview_text,
    )

    # Save machine-readable JSON
    json_file = OUTPUT_DIR / f"{stakeholder}.json"

    json_file.write_text(
        json.dumps(
            report.model_dump(),
            indent=2,
            ensure_ascii=False,
        ),
        encoding="utf-8",
    )

    # Save human-readable Markdown
    markdown_file = OUTPUT_DIR / f"{stakeholder}.md"

    markdown_file.write_text(
        create_markdown(report),
        encoding="utf-8",
    )

    print(
        f"Found {len(report.discoveries)} discoveries"
    )

    print(f"Saved -> {json_file}")
    print(f"Saved -> {markdown_file}")

    # Avoid sending all free-tier requests at once
    time.sleep(2)


print()
print("=" * 60)
print("Discovery analysis complete.")
print("=" * 60)