type ServerTextAreaInputProps = {
  currValue: string | number | readonly string[];
  label: string;
  note?: string;
  onChange: (value: string) => void;
};

export const ServerTextAreaInput = ({
  currValue,
  label,
  note,
  onChange,
}: ServerTextAreaInputProps) => {
  return (
    <>
      <label>{label}</label>
      <textarea
        defaultValue={currValue}
        className="base-input w-full"
        rows={3}
        onChange={(val) => onChange(val.target.value)}
      ></textarea>
    </>
  );
};
