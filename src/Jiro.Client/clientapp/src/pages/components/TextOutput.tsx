import { CommandResponse } from "../../api";

export const TextOutput = (props: { command: CommandResponse }) => {
  const { command } = props;

  return (
    <div className="my-1 break-words">
      <span className="text-accent">Jiro:</span> {command.result?.data}
    </div>
  );
};
