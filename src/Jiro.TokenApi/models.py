from typing import List
from pydantic import BaseModel


class Counter(BaseModel):
    text: str


class Message(BaseModel):
    role: str
    content: str


class TokenBody(BaseModel):
    messages: List[Message]
