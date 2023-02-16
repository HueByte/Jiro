import React, { useEffect, useState } from "react";
import { BiMailSend } from "react-icons/bi";
import * as api from "../api";
import { CommandType } from "../api/CommandEnum";
import jiroAvatar from "../assets/Jiro.png";
import { CommandOutputRenderer } from "./components";
import { UserCommand } from "./Models";

const Homepage = () => {
  const messageInputRef = React.useRef<HTMLInputElement | null>(null);
  const dummy = React.useRef<HTMLInputElement | null>(null);
  const chatContainer = React.useRef<HTMLInputElement | null>(null);
  const [commands, setCommands] = useState<UserCommand[]>([]);
  const [newDataAvailable, setNewDataAvailable] =
    useState<api.CommandResponse>();
  const [isFetching, setIsFetching] = useState(false);

  useEffect(() => {
    if (
      chatContainer &&
      chatContainer.current &&
      chatContainer.current.clientHeight
    ) {
      debounce(scroll, 200);
    }
  });

  useEffect(() => {
    (async () => {
      if (!newDataAvailable) {
        setIsFetching(false);
        return;
      }

      let userCommand: UserCommand = {
        prompt: commands[commands.length - 1].prompt,
        response: newDataAvailable,
        isLoading: false,
      };

      setCommands((previous) => {
        return [...previous.slice(0, -1), userCommand];
      });

      setIsFetching(false);
    })();
  }, [newDataAvailable]);

  const sendMessage = async () => {
    if (
      isFetching ||
      !messageInputRef ||
      messageInputRef.current?.value.length === 0
    )
      return;

    // clear the input field
    let promptValue = messageInputRef.current?.value;
    messageInputRef.current!.value = "";

    if (promptValue?.toLowerCase() == "$ clear") {
      setCommands([]);
      return;
    }

    setIsFetching(true);

    let userCommand: UserCommand = {
      prompt: promptValue,
      response: undefined,
      isLoading: true,
    };

    setCommands([...commands, userCommand]);

    let data: api.CommandResponse | undefined = undefined;

    try {
      let result = await api.JiroService.postApiJiro({
        requestBody: {
          prompt: promptValue,
        },
      });

      data = result.data;
    } catch (err: any) {
      data = {
        commandName: err.data,
        result: { response: err.errors.join(", ") },
        commandType: CommandType.Text,
      };
    }

    setNewDataAvailable(data);

    messageInputRef.current?.focus();
  };

  const scroll = () => {
    dummy?.current?.scrollIntoView({ behavior: "smooth" });
  };

  const debounce = (method: any, delay: number) => {
    clearTimeout(method._tId);
    method._tId = setTimeout(() => {
      method();
    }, delay);
  };

  return (
    <div className="flex w-full flex-row justify-center gap-6 px-8 pt-8 lg:flex-col md:px-4 md:pt-0">
      <div
        className={`${
          isFetching ? "shadow-lg shadow-accent3 " : ""
        }base-border-gradient-r h-fit w-[256px] flex-shrink-0 overflow-hidden rounded-full transition duration-1000 lg:mx-auto lg:w-[196px] md:w-[128px]`}
      >
        <img
          className="rounded-full bg-elementLight"
          src={jiroAvatar}
          alt="temp image"
        />
      </div>
      <div className="flex h-[90%] w-[1024px] min-w-[700px] flex-col rounded-xl bg-altBackgroundColor shadow-lg shadow-element lg:h-[calc(90%_-_196px)] lg:w-full lg:min-w-full md:h-[calc(90%_-_128px)]">
        <div
          ref={chatContainer}
          className="mx-12 my-4 flex flex-1 flex-col overflow-y-auto overflow-x-hidden pr-2 md:mx-6"
        >
          {commands?.length > 0 &&
            commands?.map((command, index) => {
              return (
                <div key={index} className="my-2 break-words">
                  <CommandOutputRenderer command={command} />
                </div>
              );
            })}
          <div ref={dummy}></div>
        </div>
        <div className="relative h-fit px-12 py-4 md:px-6">
          <input
            ref={messageInputRef}
            onKeyDown={(e) => {
              if (e.key === "Enter") sendMessage();
            }}
            type="text"
            className="base-input w-full pr-10"
            placeholder="Type a message or command..."
          />
          <BiMailSend
            onClick={sendMessage}
            className="absolute right-[56px] top-1/2 -translate-y-1/2 text-3xl hover:cursor-pointer hover:text-accent2"
          />
        </div>
      </div>
      <div className="w-[256px] min-w-0"></div>
    </div>
  );
};

export default Homepage;
