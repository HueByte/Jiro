import asyncio
import sharedStorage
import mainLoop


async def main():
    while True:
        command = input("User: ")
        result = await mainLoop.makeJiroRequest(command)
        await mainLoop.printResponseMessage(result.data)

asyncio.run(main())
