import React, { useEffect, useState } from "react";
import { BiMailSend } from "react-icons/bi";
import { FaGhost } from "react-icons/fa";
import * as api from "../../api";
import { CommandType } from "../../api/CommandEnum";
import { CommandOutputRenderer } from "../components";
import { UserCommand } from "../Models";
import MorphAvatar from "./components/MorphContainer";

const Homepage = () => {
  const messageInputRef = React.useRef<HTMLTextAreaElement | null>(null);
  const dummy = React.useRef<HTMLInputElement | null>(null);
  const chatContainer = React.useRef<HTMLInputElement | null>(null);
  const [commands, setCommands] = useState<UserCommand[]>([]);
  const [newDataAvailable, setNewDataAvailable] =
    useState<api.CommandResponse>();
  const [isFetching, setIsFetching] = useState(false);

  useEffect(() => {
    if (messageInputRef.current) {
      messageInputRef.current.style.height = "auto";
    }
  }, []);

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
      !messageInputRef.current ||
      messageInputRef.current.value.trim().length === 0
    )
      return;

    // clear the input field
    let promptValue = messageInputRef.current.value.trim();
    messageInputRef.current.value = "";
    messageInputRef.current.style.height = "auto";

    if (promptValue?.toLowerCase() == "$clear") {
      await resetSession();
      return;
    }

    setIsFetching(true);

    let userCommand: UserCommand = {
      prompt: promptValue.replace(/\n/g, "\n\n "),
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

    messageInputRef.current.focus();
  };

  const resetSession = async () => {
    await api.JiroService.postApiJiro({
      requestBody: {
        prompt: "$reset",
      },
    });

    setCommands([]);
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
    <div className="flex w-full flex-row justify-center gap-6 px-8 py-8 lg:flex-col md:px-4 md:py-0">
      <MorphAvatar />
      <div className="flex h-[100%] w-[1024px] min-w-[700px] flex-col rounded-xl bg-element shadow-lg shadow-element lg:h-[calc(90%_-_196px)] lg:w-full lg:min-w-full md:h-[calc(90%_-_128px)]">
        <div
          ref={chatContainer}
          className="mx-12 my-4 flex flex-1 flex-col overflow-y-auto overflow-x-hidden pr-2 md:mx-6"
        >
          {commands?.length > 0 ? (
            commands?.map((command, index) => {
              return (
                <div key={index} className="my-2 break-words">
                  <CommandOutputRenderer command={command} />
                </div>
              );
            })
          ) : (
            <div className="mt-10 h-full self-center text-8xl text-altBackgroundColorLight">
              <FaGhost />
            </div>
          )}
          <div ref={dummy}></div>
        </div>
        <div className="relative mx-12 my-4 max-h-32 md:px-6">
          <textarea
            ref={messageInputRef}
            onKeyDown={(e) => {
              if (e.key === "Enter" && !e.shiftKey) {
                e.preventDefault();
                sendMessage();
              }
            }}
            className="base-input bottom-0 w-full resize-none"
            placeholder="Type a message or command..."
            rows={1}
            onChange={(e) => {
              e.target.style.height = "auto";

              let realHeight = Math.max(
                e.target.offsetHeight,
                e.target.scrollHeight
              );

              let height = Math.min(realHeight, 128);
              e.target.style.height = height + "px";
            }}
          ></textarea>
          <BiMailSend
            onClick={sendMessage}
            className="absolute right-[12px] top-1/2 -translate-y-1/2 text-3xl hover:cursor-pointer hover:text-accent2"
          />
        </div>
      </div>
      <div className="w-[256px] min-w-0"></div>
    </div>
  );
};

export default Homepage;
