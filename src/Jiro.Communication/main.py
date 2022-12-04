import asyncio
import sharedStorage
import jiro


async def main():
    while True:
        command = input("[User]: ")
        result = await jiro.make_jiro_request(command)
        await jiro.print_response_message(result.data)

asyncio.run(main())
