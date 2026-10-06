import os
import time
from dotenv import load_dotenv
from google import genai
from google.genai import types
from google.genai.errors import ServerError

load_dotenv()

client = genai.Client(api_key=os.getenv("GEMINI_API_KEY"))

PASSENGER_ROLE = """
You are a Passenger Stakeholder Agent participating in
requirements discovery for SmartRail, a national railway
passenger and operations platform.

You must think only from the passenger's perspective.

Your goals are:
- complete railway journeys with minimum confusion
- receive accurate train information
- understand delays and cancellations
- understand platform changes
- know where the train currently is
- recover when travel does not go as planned

Consider different passengers including:
- daily commuters
- occasional travelers
- tourists
- elderly passengers
- passengers with disabilities

Do not design software architecture.

Your responsibility is to identify:
- passenger goals
- journeys
- information requirements
- pain points
- missing functionality
- failure scenarios
- unclear requirements
- questions for the railway organization

Challenge incomplete requirements instead of simply agreeing
with them.
"""

question = """
Imagine you are travelling from one railway station to another.

Walk through the complete passenger journey from deciding
to travel until arriving at the destination.

At every important stage explain:
1. What you need to know.
2. What you may want to do.
3. What could go wrong.
4. What SmartRail should help you with.
"""

# Configure system instructions and disable AFC warning
config = types.GenerateContentConfig(
    system_instruction=PASSENGER_ROLE,
    automatic_function_calling=types.AutomaticFunctionCallingConfig(disable=True),
)

# Retry logic for 503 errors
max_retries = 3
for attempt in range(max_retries):
    try:
        response = client.models.generate_content(
            model="gemini-2.5-flash",
            contents=question,
            config=config,
        )
        print(response.text)
        break
    except ServerError as e:
        if e.code == 503 and attempt < max_retries - 1:
            wait_time = 2 ** attempt
            print(f"Server busy (503). Retrying in {wait_time}s...")
            time.sleep(wait_time)
        else:
            raise e