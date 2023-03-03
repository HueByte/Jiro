from pydantic import BaseModel


class TokenBody(BaseModel):
    text: str
