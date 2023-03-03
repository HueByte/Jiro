import MDEditor from "@uiw/react-md-editor";
import { CommandResponse } from "../../api";
import "./styles/RendererStyles.css";

export const TextOutput = (props: { command: CommandResponse }) => {
  const { command } = props;

  return (
    <MDEditor.Markdown source={(command.result as any)?.response} style={{}} />
  );
};
