import { useState } from "react";
import { AiFillEye, AiFillEyeInvisible } from "react-icons/ai";

type ServerSettingInputProps = {
  currValue: string | number | undefined | null;
  label: string;
  note?: string;
  isSecret?: boolean;
  onChange: (value: string) => void;
};

export const ServerSettingInput = ({
  currValue,
  label,
  note,
  isSecret = false,
  onChange,
}: ServerSettingInputProps) => {
  const [isVisible, setIsVisible] = useState(false);
  return (
    <>
      <label>{label}</label>
      <div className="relative">
        <input
          type={isSecret ? (isVisible ? "text" : "password") : "text"}
          className={`base-input w-full${isSecret ? " pr-10" : ""}`}
          defaultValue={currValue ?? ""}
          onChange={(val) => onChange(val.target.value)}
        />
        {isSecret &&
          (isVisible ? (
            <AiFillEye
              onClick={() => setIsVisible(!isVisible)}
              className="absolute top-1/2 right-2 inline -translate-y-1/2 cursor-pointer"
            />
          ) : (
            <AiFillEyeInvisible
              onClick={() => setIsVisible(!isVisible)}
              className="absolute top-1/2 right-2 inline -translate-y-1/2 cursor-pointer"
            />
          ))}
      </div>
    </>
  );
};
