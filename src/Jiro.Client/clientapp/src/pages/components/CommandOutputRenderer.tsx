import { memo } from "react";
import { GraphOutput, TextOutput } from ".";
import { CommandType } from "../../api/CommandEnum";
import { UserCommand } from "../Models";

export const CommandOutputRenderer = memo(
  (props: { command?: UserCommand }) => {
    const { command } = props;

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
  }
);
