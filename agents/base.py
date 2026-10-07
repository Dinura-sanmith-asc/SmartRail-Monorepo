import os
import time
import random

from dotenv import load_dotenv
from google import genai
from google.genai import types
from google.genai.errors import ServerError


load_dotenv()

client = genai.Client(
    api_key=os.getenv("GEMINI_API_KEY")
)


def run_agent(role, question, model="gemini-2.5-flash", max_retries=3):
    """
    Runs one stakeholder agent interview.
    """

    config = types.GenerateContentConfig(
        system_instruction=role,
        automatic_function_calling=types.AutomaticFunctionCallingConfig(
            disable=True
        ),
    )

    for attempt in range(max_retries):
        try:
            response = client.models.generate_content(
                model=model,
                contents=question,
                config=config,
            )

            return response.text

        except ServerError as e:
            if e.code == 503 and attempt < max_retries - 1:
                wait_time = 2 ** attempt

                print(
                    f"Server busy (503). "
                    f"Retrying in {wait_time}s..."
                )

                time.sleep(wait_time)

            else:
                raise

def run_structured_agent(
    role,
    question,
    schema,
    max_retries=2,
):
    """
    Runs an agent with structured output.

    Uses free-tier lightweight Gemini models and
    automatically falls back if one model is overloaded.
    """

    models = [
        "gemini-3.5-flash-lite",
        "gemini-3.1-flash-lite",
        "gemini-2.5-flash-lite",
    ]

    config = types.GenerateContentConfig(
        system_instruction=role,
        response_mime_type="application/json",
        response_schema=schema,
        automatic_function_calling=types.AutomaticFunctionCallingConfig(
            disable=True
        ),
    )

    last_error = None

    for model in models:

        print(f"Using model: {model}")

        for attempt in range(max_retries):

            try:
                response = client.models.generate_content(
                    model=model,
                    contents=question,
                    config=config,
                )

                if response.parsed is not None:
                    return response.parsed

                return schema.model_validate_json(
                    response.text
                )

            except ServerError as e:
                last_error = e

                if e.code == 503:
                    wait_time = (
                        (2 ** attempt) * 3
                        + random.uniform(0, 2)
                    )

                    print(
                        f"{model} unavailable (503). "
                        f"Retrying in "
                        f"{wait_time:.1f}s..."
                    )

                    time.sleep(wait_time)

                else:
                    raise

        print(
            f"{model} still unavailable. "
            f"Trying fallback model..."
        )

    raise last_error