import { useEffect, useState } from "react";

export const TextOutput = (props: { command: any }) => {
  const { command } = props;
  const [text, setText] = useState<string>("");

  useEffect(() => {
    (async () => {
      for (let index = 0; index <= command.result.response.length; index++) {
        setText(command.result.response.slice(0, text?.length + index));
        await sleep(10);
      }
    })();
  }, []);

  const sleep = (ms: number) => new Promise((r) => setTimeout(r, ms));

  return (
    <div className="my-1 break-words">
      <span className="text-accent">Jiro:</span> {text}
    </div>
  );
};
