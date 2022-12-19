import requests
import sys
import sharedStorage
import asyncio
import json
import lib
from models import jiro_models


async def send_request(prompt: str) -> jiro_models.JiroResponse:
    body = jiro_models.JiroRequest(prompt)
    requestUrl = sharedStorage.config['jiroUrl']

    response = requests.post(
        f'{requestUrl}/api/jiro', json={"prompt": body.prompt})

    return jiro_models.JiroResponse(response.text)


async def print_response_message(message: str) -> None:
    sys.stdout.write(f"\n{lib.colors.JIRO}[Jiro]$ ")
    sys.stdout.flush()

    speed = sharedStorage.config['chatSpeed']

    for char in message:
        sys.stdout.write(char)
        sys.stdout.flush()
        await asyncio.sleep(speed)

    print(f'{lib.colors.ENDC}\n')
