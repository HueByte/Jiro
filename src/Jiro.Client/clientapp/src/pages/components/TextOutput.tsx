import MDEditor from "@uiw/react-md-editor";
import "./styles/RendererStyles.css";

export const TextOutput = (props: { command: any }) => {
  const { command } = props;

  return (
    <div className="my-1 break-words">
      <span className="text-accent">Jiro: </span>
      <MDEditor.Markdown
        source={command.result?.response}
        style={{ whiteSpace: "pre-wrap" }}
      />
    </div>
  );
};
