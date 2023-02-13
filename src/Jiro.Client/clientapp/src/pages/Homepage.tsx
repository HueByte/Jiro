import React, { useEffect, useState } from "react";
import { BiMailSend } from "react-icons/bi";
import { JiroService } from "../api";
import jiroAvatar from "../assets/Jiro.png";

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

    // clear the input field
    let local = messageInputRef.current?.value;
    messageInputRef!.current!.value = "";

    // append user message to the chat
    setMessages([...messages, local as string]);
    setIsFetching(true);

    let result = await JiroService.postApiJiro({
      requestBody: {
        prompt: local,
      },
    });

    setIsFetching(false);

    // append response to the chat
    setMessages((previous) => [...previous, result?.data?.result?.data]);

    // scroll to the bottom of the chat
    messageInputRef.current?.focus();
  };

  return (
    <div className="flex w-full flex-row justify-center gap-6 px-8 pt-8 lg:flex-col">
      <div
        className={`${
          isFetching ? "shadow-lg shadow-accent3 " : ""
        }base-border-gradient-r h-fit w-[256px] flex-shrink-0 overflow-hidden rounded-full transition duration-1000 lg:mx-auto lg:w-[196px]`}
      >
        <img
          className="rounded-full bg-elementLight"
          src={jiroAvatar}
          alt="temp image"
        />
      </div>
      <div className="flex h-[90%] w-[1024px] min-w-[700px] flex-col rounded-xl bg-altBackgroundColor shadow-lg shadow-element lg:h-[calc(90%_-_196px)] lg:w-full lg:min-w-full">
        <div
          ref={chatContainerRef}
          className="mx-12 my-4 flex flex-1 flex-col overflow-y-auto overflow-x-hidden pr-2"
        >
          {messages.map((message, index) => {
            let author = index % 2 === 0 ? "Me" : "Jiro";
            return (
              <div key={index} className="my-1 break-words">
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
