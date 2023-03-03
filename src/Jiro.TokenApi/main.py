from typing import List
import tiktoken
from fastapi import FastAPI
from models import TokenBody
from models import Counter
from models import Message

app = FastAPI()


@app.get("/")
async def read_root():
    return {"Hello": "World"}


@app.post("/reduce")
async def tokenizer(input: TokenBody) -> List[Message]:
    encoding = tiktoken.encoding_for_model("gpt-3.5-turbo")
    messages = input.messages
    num_tokens = 0

    while (True):
        combinedText = ' '.join(
            [message.content for message in messages])
        num_tokens = len(encoding.encode(combinedText))
        print(f'Number of tokens: {num_tokens}')

        if (num_tokens > 300 and len(messages) > 3):
            print('Too many tokens, attempting to reduce')
            messages.pop(2)
            messages.pop(1)
        else:
            break

    return messages


@app.post("/tokenize")
async def tokenizer(input: Counter) -> int:
    encoding = tiktoken.encoding_for_model("gpt-3.5-turbo")
    num_tokens = len(encoding.encode(input.text))

    print(f'Number of tokens: {num_tokens}')

    return num_tokens
