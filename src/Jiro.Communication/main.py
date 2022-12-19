import asyncio
import sharedStorage
import jiro
import lib
import graphs


async def main():
    print(lib.logo)
    username = lib.get_username()

    while True:
        try:
            prompt = input(f"{lib.colors.USER}[{username}]$ ")
            print(lib.colors.ENDC, end=" ")

            if (prompt):
                if (prompt.lower() == "exit"):
                    break

                response = await jiro.send_request(prompt)
                await jiro.print_response_message(response.result['data'])
            else:
                await jiro.print_response_message("Give me some message first")
        except BaseException as ex:
            await jiro.print_response_message("Something went wrong, try again.\n" + str(ex))


asyncio.run(main())
