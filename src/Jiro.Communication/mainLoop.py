import requests
import sys
import sharedStorage
import asyncio
import json


async def makeJiroRequest(prompt):
    body = jiroRequest(prompt)
    bodyJson = {"prompt": body.prompt}
    requestUrl = sharedStorage.config['jiroUrl']

    response = requests.post(f'{requestUrl}/api/jiro', json=bodyJson)
    return jiroResponse(response.content)


async def printResponseMessage(message):
    sys.stdout.write("Jiro: ")
    sys.stdout.flush()
    for char in message:
        sys.stdout.write(char)
        sys.stdout.flush()
        await asyncio.sleep(0.02)
    print()


class jiroRequest:
    def __init__(self, data):
        self.prompt = data


class jiroResponse:
    def __init__(self, data, isSuccess, errors):
        self.data = data
        self.isSuccess = isSuccess
        self.errors = errors

    def __init__(self, jsonString):
        self.__dict__ = json.loads(jsonString)
