export const TextOutput = (props: { command: any }) => {
  const { command } = props;

  return (
    <div className="my-1 break-words">
      <span className="text-accent">Jiro:</span> {command.result?.response}
    </div>
  );
};
