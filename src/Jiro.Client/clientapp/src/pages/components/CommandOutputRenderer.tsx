import { memo } from "react";
import { GraphOutput, TextOutput } from ".";
import { CommandType } from "../../api/CommandEnum";
import { UserCommand } from "../Models";

export const CommandOutputRenderer = memo(
  (props: { command?: UserCommand }) => {
    const { command } = props;

    // match rendere to command type
    if (command?.response?.commandType === CommandType.Text) {
      return (
        <div className="flex flex-col">
          <div className="w-full">
            <span className="text-primary">Me:</span> {command.prompt}
          </div>
          <div className="w-full">
            <TextOutput command={command.response} />
          </div>
        </div>
      );
    } else if (command?.response?.commandType === CommandType.Graph) {
      return (
        <div className="flex flex-col">
          <div className="w-full">
            <span className="text-primary">Me:</span> {command.prompt}
          </div>
          <div className="w-full">
            <GraphOutput command={command.response} />
          </div>
        </div>
      );
    } else {
      return (
        <div className="flex flex-col">
          <div className="w-full">
            <span className="text-primary">Me:</span> {command?.prompt}
          </div>
          <div className="w-full">
            {command?.isLoading ? (
              <div>
                <span className="animate-pulse text-accent">Jiro: </span>...
              </div>
            ) : (
              <div>Something went wrong</div>
            )}
          </div>
        </div>
      );
    }
  }
);
