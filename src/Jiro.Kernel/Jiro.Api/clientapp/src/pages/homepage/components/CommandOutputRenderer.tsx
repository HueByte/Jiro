import MDEditor from "@uiw/react-md-editor";
import { memo } from "react";
import { BsFillCloudHazeFill } from "react-icons/bs";
import { GraphOutput, TextOutput } from ".";
import { CommandType } from "../../../api/CommandEnum";
import { UserCommand } from "../../Models";
import "./styles/RendererStyles.css";

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
          <div className="animate-pulse italic">Thinking...</div>
        ) : (
          <div>Something went wrong</div>
        );
      }
    };

    return (
      <div className="flex flex-col">
        <div className="mb-4 w-full">
          <span className="mb-2 grid w-20 place-items-center rounded-lg bg-backgroundColorLight p-2 text-accent7">
            Me
          </span>
          <MDEditor.Markdown source={command?.prompt} style={{}} />
        </div>
        <div className="my-1 w-full break-words">
          <div
            className={`${
              command?.isLoading ? "animate-pulse " : ""
            }mb-2 flex w-20 items-center justify-center gap-1 rounded-lg bg-backgroundColorLight p-2 text-accent`}
          >
            <BsFillCloudHazeFill className="inline" /> Jiro
          </div>
          {getComponent()}
        </div>
      </div>
    );
  }
);
