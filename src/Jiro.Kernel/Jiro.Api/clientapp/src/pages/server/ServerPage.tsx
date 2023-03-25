import { ReactNode } from "react";

const ServerPage = () => {
  return (
    <div className="flex h-fit min-h-full w-full justify-center overflow-visible px-8 py-8">
      <div className="flex min-h-full w-5/6 flex-col gap-2 rounded-xl bg-element p-2 pt-6 shadow-lg shadow-element">
        <ServerSettingBox name="Main Settings">
          <label>urls</label>
          <input type="text" className="base-input w-full" />
          <label>Tokenizer url</label>
          <input type="text" className="base-input w-full" />
          <label>Allowed Hosts</label>
          <input type="text" className="base-input w-full" />
          <label>Whitelist</label>
          <input
            id="default-checkbox"
            type="checkbox"
            value=""
            className="text-blue-600 bg-gray-100 border-gray-300 focus:ring-blue-500 dark:focus:ring-blue-600 dark:ring-offset-gray-800 dark:bg-gray-700 dark:border-gray-600 h-4 w-4 rounded focus:ring-2"
          />
        </ServerSettingBox>

        <div className="p-2">
          <div>Setting</div>
          <label>Input</label>
          <input type="text" className="base-input w-full" />
          <label>Input</label>
          <input type="text" className="base-input w-full" />
          <label>Input</label>
          <input type="text" className="base-input w-full" />
        </div>
        <div className="p-2">
          <div>Setting</div>
          <label>Input</label>
          <input type="text" className="base-input w-full" />
        </div>
      </div>
    </div>
  );
};

type Props = {
  name: string;
  children: string | JSX.Element | JSX.Element[];
};

const ServerSettingBox = ({ name, children }: Props) => {
  return (
    <div className="relative flex flex-col gap-2 rounded bg-backgroundColor p-2 pt-8">
      <div className="absolute -top-4 left-10 rounded bg-element p-2">
        {name}
      </div>
      {children}
    </div>
  );
};

// const SettingInput = ({})

export default ServerPage;
