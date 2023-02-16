import MDEditor from "@uiw/react-md-editor";
import { CommandResponse } from "../../api";
import "./styles/RendererStyles.css";

export const TextOutput = (props: { command: CommandResponse }) => {
  const { command } = props;

  return (
    <div className="my-1 break-words">
      <span className="text-accent">Jiro: </span>
      <MDEditor.Markdown
        source={(command.result as any)?.response}
        style={{ whiteSpace: "pre-wrap" }}
      />
    </div>
  );
};
