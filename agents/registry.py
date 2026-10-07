from agents import passenger
from agents import driver
from agents import station_manager
from agents import railway_controller
from agents import catering_provider
from agents import customer_service
from agents import product_owner


AGENTS = {
    "passenger": passenger,
    "driver": driver,
    "station_manager": station_manager,
    "railway_controller": railway_controller,
    "catering_provider": catering_provider,
    "customer_service": customer_service,
    "product_owner": product_owner,
}


def ask_agent(agent_name, question):

    if agent_name not in AGENTS:
        raise ValueError(
            f"Unknown stakeholder agent: {agent_name}"
        )

    return AGENTS[agent_name].interview(
        question
    )