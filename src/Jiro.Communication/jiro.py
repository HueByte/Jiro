import requests
import sys
import sharedStorage
import asyncio
import json
import os
import pwd


def get_username():
    return pwd.getpwuid(os.getuid())[0]


async def make_jiro_request(prompt):
    body = JiroRequest(prompt)
    requestUrl = sharedStorage.config['jiroUrl']

    response = requests.post(
        f'{requestUrl}/api/jiro', json={"prompt": body.prompt})

    return JiroResponse(response.content)


async def print_response_message(message):
    sys.stdout.write("[Jiro]: ")
    sys.stdout.flush()
    for char in message:
        sys.stdout.write(char)
        sys.stdout.flush()
        await asyncio.sleep(0.02)
    print('\n\n')


class JiroRequest:
    def __init__(self, data):
        self.prompt = data


class JiroResponse:
    def __init__(self, data, isSuccess, errors):
        self.data = data
        self.isSuccess = isSuccess
        self.errors = errors

    def __init__(self, jsonString):
        self.__dict__ = json.loads(jsonString)
