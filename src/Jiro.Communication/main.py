import asyncio
import sharedStorage
import jiro
import lib


async def main():
    print(lib.logo)
    username = lib.get_username()
    while True:
        prompt = input(f"{lib.colors.USER}[{username}]$ ")
        print(lib.colors.ENDC, end=" ")

        result = await jiro.send_request(prompt)
        await jiro.print_response_message(result.data)

asyncio.run(main())
