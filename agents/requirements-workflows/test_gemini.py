import os
import time
from dotenv import load_dotenv
from google import genai
from google.genai.errors import ServerError

load_dotenv()

client = genai.Client(api_key=os.getenv("GEMINI_API_KEY"))
chat = client.chats.create(model="gemini-3.5-flash-lite")

prompt = "Explain in one short sentence what a railway passenger needs from a digital railway application."

max_retries = 3
for attempt in range(max_retries):
    try:
        response = chat.send_message(prompt)
        print(response.text)
        break
    except ServerError as e:
        if e.code == 503 and attempt < max_retries - 1:
            print(f"Model busy (503). Retrying in {2 ** attempt} seconds...")
            time.sleep(2 ** attempt)
        else:
            raise e