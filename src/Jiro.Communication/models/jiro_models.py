import json
from enum import Enum


class CommandTypeEnum(Enum):
    OTHER = 1
    CHAT = 2
    WEATHER = 3


class CommandResult:
    data: str


class JiroRequest:
    prompt: str

    def __init__(self, data: str):
        self.prompt = data


class JiroResponse:
    result: CommandResult
    isSuccess: bool
    errors: list[str]

    def __init__(self, result: CommandResult, isSuccess: bool, errors: list[str]):
        self.result = result
        self.isSuccess = isSuccess
        self.errors = errors

    def __init__(self, jsonString: str):
        self.__dict__ = json.loads(jsonString)
