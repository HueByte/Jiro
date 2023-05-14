type ServerSettingBoxProps = {
  icon: JSX.Element;
  name: string;
  children: string | JSX.Element | JSX.Element[];
};

export const ServerSettingBox = ({
  icon,
  name,
  children,
}: ServerSettingBoxProps) => {
  return (
    <div className="relative flex flex-[40%] flex-col gap-2 rounded bg-backgroundColor p-2 pt-8 md:flex-[100%]">
      <div className="absolute -top-4 left-10 rounded bg-element px-2 py-1 font-bold text-accent7 md:text-sm">
        {icon} {name}
      </div>
      {children}
    </div>
  );
};
