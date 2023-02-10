import { useState } from "react";

const Homepage = () => {
  const [messages, setMessages] = useState([{}]);

  return (
    <div className="mx-auto flex h-full gap-6 pt-4">
      <div className="h-fit w-[256px] overflow-hidden rounded-full bg-elementLight p-2">
        <img
          src="https://cdn.discordapp.com/avatars/215556401467097088/3342856edc77dcb72bda6c94de4592cc.png?size=256"
          alt="temp image"
        />
      </div>
      <div className="flex h-[90%] w-[760px] flex-1 flex-col rounded-xl bg-altBackgroundColor shadow-lg shadow-element">
        <div className="flex-1 p-4">
          {messages.map((message, index) => (
            <div key={index}>message</div>
          ))}
        </div>
        <div className="h-fit p-4">
          <input type="text" className="mts-input w-full" />
        </div>
      </div>
    </div>
  );
};

export default Homepage;
