import React, { useEffect, useRef, useState } from "react";
import { BiMailSend } from "react-icons/bi";
import { JiroService } from "../api";

const Homepage = () => {
  const [messages, setMessages] = useState<string[]>([]);
  const messageInputRef = React.useRef<HTMLInputElement | null>(null);
  const chatContainerRef = React.useRef<HTMLDivElement | null>(null);
  const [isFetching, setIsFetching] = useState(false);

  useEffect(() => {
    chatContainerRef.current?.scrollTo(
      0,
      chatContainerRef.current.scrollHeight
    );
  }, []);

  useEffect(() => {
    chatContainerRef.current?.scrollTo(
      0,
      chatContainerRef.current.scrollHeight
    );
  }, [isFetching]);

  const sendMessage = async () => {
    if (
      isFetching ||
      !messageInputRef ||
      messageInputRef.current?.value.length === 0
    )
      return;

    let local = messageInputRef.current?.value;
    messageInputRef!.current!.value = "";

    setMessages([...messages, local as string]);
    setIsFetching(true);

    let result = await JiroService.postApiJiro({
      requestBody: {
        prompt: local,
      },
    });

    setIsFetching(false);
    setMessages((previous) => [...previous, result?.data?.result?.data]);
    messageInputRef.current?.focus();
  };

  return (
    <div className="mx-4 flex w-full flex-row justify-center gap-6 pt-8">
      <div
        className={`${
          isFetching ? "shadow-lg shadow-accent3 " : ""
        }base-border-gradient-r h-fit w-[256px] flex-shrink-0 overflow-hidden rounded-full transition duration-1000`}
      >
        <img
          className="rounded-full bg-elementLight"
          src="https://cdn.discordapp.com/avatars/215556401467097088/3342856edc77dcb72bda6c94de4592cc.png?size=256"
          alt="temp image"
        />
      </div>
      <div className="flex h-[90%] w-[1024px] min-w-[700px] flex-col rounded-xl bg-altBackgroundColor shadow-lg shadow-element">
        <div
          ref={chatContainerRef}
          className="mx-12 my-4 flex flex-1 flex-col overflow-y-auto break-words pr-2"
        >
          {messages.map((message, index) => {
            let author = index % 2 === 0 ? "Me" : "Jiro";
            return (
              <div key={index} className="my-1">
                <>
                  <span
                    className={`${
                      index % 2 ? "text-accent3 " : "text-accent "
                    }font-bold`}
                  >
                    {author}
                  </span>
                  : {message}
                </>
              </div>
            );
          })}
        </div>
        <div className="relative h-fit px-12 py-4">
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
