type ServerCheckboxInputProps = {
  currValue: boolean | null | undefined;
  label: string;
  note?: string;
  onChange: (value: boolean) => void;
};

export const ServerCheckboxInput = ({
  currValue,
  label,
  note,
  onChange,
}: ServerCheckboxInputProps) => {
  return (
    <div className="flex items-center gap-2">
      <label className="text-gray-900 dark:text-gray-300 font-medium">
        {label}
      </label>
      <input
        autoComplete="off"
        type="checkbox"
        checked={currValue ?? false}
        onChange={(val) => onChange(val.target.checked)}
        className="text-secondaryLight-600 bg-gray-100 border-gray-300 focus:ring-accent-500 dark:ring-offset-gray-800 dark:bg-gray-700 dark:border-gray-600 h-4 w-4 rounded hover:cursor-pointer focus:ring-2"
      />
    </div>
  );
};
