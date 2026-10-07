from pathlib import Path

from agents import passenger
from agents import driver
from agents import station_manager
from agents import railway_controller
from agents import catering_provider
from agents import customer_service
from agents import product_owner


OUTPUT_DIR = Path("outputs/individual")

OUTPUT_DIR.mkdir(
    parents=True,
    exist_ok=True
)


interviews = [
    (
        "passenger",
        passenger,
        """
Walk through a complete railway passenger journey from
planning the journey until reaching the destination.

At each important stage identify:

1. What you need to know.
2. What you want to do.
3. What could go wrong.
4. What SmartRail could reasonably help with.
5. What questions remain unanswered.

Do not assume every desirable feature belongs in the final
SmartRail scope.
"""
    ),

    (
        "driver",
        driver,
        """
Walk through a complete working journey from receiving
a train assignment until the journey is completed.

At each stage identify:

1. Information you require.
2. Actions you perform.
3. Information you report.
4. Things that could go wrong.
5. Actions requiring another person's authority.
6. Missing or unclear requirements.
"""
    ),

    (
        "station_manager",
        station_manager,
        """
Walk through a normal operating day at a railway station.

Explain how SmartRail would affect:

1. Train arrivals and departures.
2. Platform management.
3. Passenger information.
4. Station displays.
5. Disruptions.
6. Accessibility facilities.
7. Local operational incidents.

Identify unclear responsibilities and failure scenarios.
"""
    ),

    (
        "railway_controller",
        railway_controller,
        """
Walk through how central railway operations manages
active train journeys.

Consider:

1. Starting journeys.
2. Monitoring train progress.
3. Delays.
4. Platform changes.
5. Cancellations.
6. Route changes.
7. Unexpected journey termination.
8. Operational incidents.

For every important action identify who should have
authority to perform it.
"""
    ),

    (
        "catering_provider",
        catering_provider,
        """
Walk through a complete food-order lifecycle where a
passenger orders food for collection during a train journey.

Consider:

1. Discovering available food.
2. Selecting a collection station.
3. Creating the order.
4. Preparing the order.
5. Train delay.
6. Train cancellation.
7. Passenger missing collection.
8. Order cancellation.
9. Successful collection.
10. Integration failure.

Identify unclear ownership between SmartRail and the
external catering platform.
"""
    ),
    (
    "customer_service",
    customer_service,
    """
Walk through how customer service supports passengers during
both normal and disrupted railway journeys.

Consider:

1. Passenger information enquiries.
2. Delays.
3. Train cancellations.
4. Missed connections.
5. Platform changes.
6. Accessibility problems.
7. Catering-related passenger problems.
8. Incorrect or conflicting information.
9. Complaints and escalation.

Identify information you need, actions you perform,
things you cannot control, escalation paths, failure
scenarios, and unclear responsibilities.
"""
    ),

(
    "product_owner",
    product_owner,
    """
Review SmartRail as the product owner after hearing the
initial business problem.

Identify:

1. The most important business outcomes.
2. Essential capabilities.
3. Important missing actors or processes.
4. Ambiguous requirements.
5. Stakeholder requests that may unnecessarily expand scope.
6. Capabilities that belong in an MVP.
7. Capabilities that could reasonably be deferred.
8. Important questions that must be answered before
   implementation.

Do not design the technical architecture and do not make
final architecture decisions.
"""
),
]


for name, agent, question in interviews:

    print()
    print("=" * 60)
    print(f"Interviewing: {name}")
    print("=" * 60)

    output_file = OUTPUT_DIR / f"{name}.md"

    if output_file.exists():
        print(f"Already exists -> {output_file}")
        print("Skipping API call.")
        continue

    result = agent.interview(question)

    output_file.write_text(
        f"# {name.replace('_', ' ').title()} Interview\n\n"
        f"{result}",
        encoding="utf-8"
    )

    print(f"Saved -> {output_file}")