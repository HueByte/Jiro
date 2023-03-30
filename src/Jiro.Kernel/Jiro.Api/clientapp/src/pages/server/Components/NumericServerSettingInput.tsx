import { ChangeEvent } from "react";

type NumericServerSettingInputProps = {
  currValue: string | number | undefined | null;
  label: string;
  note?: string;
  min?: number;
  max?: number;
  onChange: (value: number) => void;
};

export const NumericServerSettingInput = ({
  currValue,
  label,
  note,
  min,
  max,
  onChange,
}: NumericServerSettingInputProps) => {
  const onNumberChange = (e: ChangeEvent<HTMLInputElement>) => {
    const value = !Number.isNaN(e.target.valueAsNumber)
      ? e.target.valueAsNumber
      : 0;

    if (min && value < min) {
      return min;
    }

    if (max && value > max) {
      return max;
    }

    return value;
  };

  return (
    <>
      <label>{label}</label>
      <input
        type="number"
        className="base-input w-full"
        defaultValue={currValue ?? ""}
        min={0}
        onChange={(val) => onChange(onNumberChange(val))}
      />
    </>
  );
};
