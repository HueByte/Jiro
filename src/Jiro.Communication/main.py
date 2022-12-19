import asyncio
import sharedStorage
import jiro
import lib
import graphs
from models import jiro_models


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
                await display_result(response)
            else:
                await jiro.print_response_message("Give me some message first")
        except BaseException as ex:
            await jiro.print_response_message("Something went wrong, try again.\n" + str(ex))


async def display_result(jiro_response: jiro_models.JiroResponse):
    if (jiro_response.isSuccess):
        if (jiro_response.commandName == 'weather'):
            graphs.display_weather(jiro_response.result['data'])
        else:
            await jiro.print_response_message(jiro_response.result['data'])
    else:
        await jiro.print_response_message(' '.join(jiro_response.errors))


asyncio.run(main())
