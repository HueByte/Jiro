import { memo } from "react";
import { GraphOutput, TextOutput } from ".";
import { CommandType } from "../../api/CommandEnum";
import { UserCommand } from "../Models";

export const CommandOutputRenderer = memo(
  (props: { command?: UserCommand }) => {
    const { command } = props;

    const getComponent: any = () => {
      if (command?.response?.commandType === CommandType.Text) {
        return <TextOutput command={command.response} />;
      } else if (command?.response?.commandType === CommandType.Graph) {
        return <GraphOutput command={command.response} />;
      } else {
        return command?.isLoading ? (
          <div>
            <span className="animate-pulse text-accent">Jiro: </span>...
          </div>
        ) : (
          <div>Something went wrong</div>
        );
      }
    };

    return (
      <div className="flex flex-col">
        <div className="w-full">
          <span className="text-primary">Me: </span> {command?.prompt}
        </div>
        <div className="w-full">{getComponent()}</div>
      </div>
    );
  }
);
