import tiktoken
from fastapi import FastAPI
from models import TokenBody

app = FastAPI()


@app.get("/")
async def read_root():
    return {"Hello": "World"}


@app.post("/tokenize")
async def tokenizer(input: TokenBody) -> int:
    encoding = tiktoken.encoding_for_model("gpt-3.5-turbo")
    num_tokens = len(encoding.encode(input.text))
    print(f'Number of tokens: {num_tokens}')
    return num_tokens
