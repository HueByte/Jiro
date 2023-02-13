import { memo } from "react";
import { useEffect } from "react";
import { CommandResponse } from "../../api";
import { CommandType } from "../../api/CommandEnum";
import { UserCommand } from "../Models";

const CommandOutputRenderer = memo((props: { command?: UserCommand }) => {
  const { command } = props;
  useEffect(() => console.log("Invoked"));

  if (command?.response?.commandType === CommandType.Text) {
    return (
      <>
        <div>
          <span className="text-primary">Me:</span> {command.prompt}
        </div>
        <TextOutput command={command.response} />
      </>
    );
  } else if (command?.response?.commandType === CommandType.Graph) {
    return (
      <>
        <div>
          <span className="text-primary">Me:</span> {command.prompt}
        </div>
        <GraphOutput command={command.response} />
      </>
    );
  } else {
    return (
      <>
        <div>
          <span className="text-primary">Me:</span> {command?.prompt}
          <div>
            {command?.isLoading ? (
              <div>
                <span className="text-accent">Jiro: </span>...
              </div>
            ) : (
              <span>Something went wrong</span>
            )}
          </div>
        </div>
      </>
    );
  }
});

const TextOutput = (props: { command: CommandResponse }) => {
  const { command } = props;

  return (
    <div className="my-1 break-words">
      <span className="text-accent">Jiro:</span> {command.result?.data}
    </div>
  );
};

const GraphOutput = (props: { command: CommandResponse }) => {
  const { command } = props;

  return (
    <div className="my-1 break-words">
      <span className="text-accent">Jiro:</span> {command.commandName}
    </div>
  );
};

export default CommandOutputRenderer;
