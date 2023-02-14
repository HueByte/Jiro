import { useEffect, useState } from "react";

export const TextOutput = (props: { command: any }) => {
  const { command } = props;
  const [text, setText] = useState<string>("");

  return (
    <div className="my-1 break-words">
      <span className="text-accent">Jiro:</span>
      {command.result?.response}
    </div>
  );
};
