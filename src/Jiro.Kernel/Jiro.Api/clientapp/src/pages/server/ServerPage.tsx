import { ChangeEvent, useEffect, useState } from "react";
import { InstanceConfigDTO, ServerService } from "../../api";

const ServerPage = () => {
  const [defaultValue, setDefaultValue] = useState<InstanceConfigDTO | null>(
    null
  );
  const [config, setConfig] = useState<InstanceConfigDTO | null>(null);

  useEffect(() => {
    (async () => {
      let config = await ServerService.getApiServer();
      console.log(config);
      if (config.data) {
        setDefaultValue(config.data);
        setConfig(config.data);
      }
    })();
  }, []);

  useEffect(() => console.log(config), [config]);

  const updateServer = async () => {
    if (config) {
      let result = await ServerService.putApiServer({ requestBody: config });
      if (result.isSuccess) {
        setDefaultValue(config);
        // TODO success modal
      }
    }
  };

  const resetToDefaults = () => {
    setConfig(defaultValue);
  };

  return (
    <div className="flex h-full w-full justify-center px-8 py-8">
      <div className="flex h-full w-5/6 flex-col rounded-xl bg-element p-2 pt-6 shadow-lg shadow-element">
        <div className="flex-1 overflow-y-scroll p-2">
          <div className="flex w-full flex-row flex-wrap gap-4 ">
            <ServerSettingBox name="Main Settings">
              <ServerSettingInput
                label="urls"
                currValue={config?.urls}
                onChange={(val) =>
                  setConfig((prev) => ({ ...prev, urls: val }))
                }
              />
              <ServerSettingInput
                label="Tokenizer Url"
                currValue={config?.TokenizerUrl}
                onChange={(val) =>
                  setConfig((prev) => ({ ...prev, TokenizerUrl: val }))
                }
              />
              <ServerSettingInput
                label="Allowed Hosts"
                currValue={config?.AllowedHosts}
                onChange={(val) =>
                  setConfig((prev) => ({ ...prev, AllowedHosts: val }))
                }
              />
              <ServerCheckboxInput
                label="Whitelist"
                currValue={config?.Whitelist}
                onChange={(val) =>
                  setConfig((prev) => ({ ...prev, Whitelist: val }))
                }
              />
            </ServerSettingBox>

            <ServerSettingBox name="Authentication Settings">
              <ServerSettingInput
                label="Issuer"
                currValue={config?.JWT?.Issuer}
                onChange={(val) =>
                  setConfig((prev) => ({
                    ...prev,
                    JWT: { ...prev?.JWT, Issuer: val },
                  }))
                }
              />
              <ServerSettingInput
                label="Audience"
                currValue={config?.JWT?.Audience}
                onChange={(val) =>
                  setConfig((prev) => ({
                    ...prev,
                    JWT: { ...prev?.JWT, Audience: val },
                  }))
                }
              />
              <ServerSettingInput
                label="Secret"
                currValue={config?.JWT?.Secret}
                onChange={(val) =>
                  setConfig((prev) => ({
                    ...prev,
                    JWT: { ...prev?.JWT, Secret: val },
                  }))
                }
              />
              <NumericServerSettingInput
                label="Access Token Expire Time"
                currValue={config?.JWT?.AccessTokenExpireTime}
                min={1}
                onChange={(val) =>
                  setConfig((prev) => ({
                    ...prev,
                    JWT: {
                      ...prev?.JWT,
                      AccessTokenExpireTime: val,
                    },
                  }))
                }
              />
              <NumericServerSettingInput
                label="Access Token Expire Time"
                currValue={config?.JWT?.RefreshTokenExpireTime}
                min={5}
                onChange={(val) =>
                  setConfig((prev) => ({
                    ...prev,
                    JWT: {
                      ...prev?.JWT,
                      RefreshTokenExpireTime: val,
                    },
                  }))
                }
              />
            </ServerSettingBox>

            <ServerSettingBox name="Logger Settings">
              <ServerSettingInput
                label="Time Interval"
                currValue={config?.Log?.TimeInterval}
                onChange={(val) =>
                  setConfig((prev) => ({
                    ...prev,
                    Log: { ...prev?.Log, TimeInterval: val },
                  }))
                }
              />
              <ServerSettingInput
                label="Main Log Level"
                currValue={config?.Log?.LogLevel}
                onChange={(val) =>
                  setConfig((prev) => ({
                    ...prev,
                    Log: { ...prev?.Log, LogLevel: val },
                  }))
                }
              />
              <ServerSettingInput
                label="ASP.NET Core Log Level"
                currValue={config?.Log?.AspNetCoreLevel}
                onChange={(val) =>
                  setConfig((prev) => ({
                    ...prev,
                    Log: { ...prev?.Log, AspNetCoreLevel: val },
                  }))
                }
              />
              <ServerSettingInput
                label="Database Log Level"
                currValue={config?.Log?.DatabaseLevel}
                onChange={(val) =>
                  setConfig((prev) => ({
                    ...prev,
                    Log: { ...prev?.Log, DatabaseLevel: val },
                  }))
                }
              />
              <ServerSettingInput
                label="System Log Level"
                currValue={config?.Log?.DatabaseLevel}
                onChange={(val) =>
                  setConfig((prev) => ({
                    ...prev,
                    Log: { ...prev?.Log, SystemLevel: val },
                  }))
                }
              />
            </ServerSettingBox>

            <ServerSettingBox name="GPT">
              <ServerCheckboxInput
                label="Enable GPT Chat"
                currValue={config?.Gpt?.Enable}
                onChange={(val) =>
                  setConfig((prev) => ({
                    ...prev,
                    Gpt: { ...prev?.Log, Enable: val },
                  }))
                }
              />
              <ServerSettingInput
                label="OpenAI Url"
                currValue={config?.Gpt?.BaseUrl}
                onChange={(val) =>
                  setConfig((prev) => ({
                    ...prev,
                    Gpt: { ...prev?.Log, BaseUrl: val },
                  }))
                }
              />
              <ServerSettingInput
                label="OpenAI Authentication Token"
                currValue={config?.Gpt?.AuthToken}
                onChange={(val) =>
                  setConfig((prev) => ({
                    ...prev,
                    Gpt: { ...prev?.Log, AuthToken: val },
                  }))
                }
              />
              <ServerSettingInput
                label="OpenAI Organization"
                currValue={config?.Gpt?.Organization}
                onChange={(val) =>
                  setConfig((prev) => ({
                    ...prev,
                    Gpt: { ...prev?.Log, Organization: val },
                  }))
                }
              />
              <ServerSettingInput
                label="ChatGPT System Message"
                currValue={config?.Gpt?.ChatGpt?.SystemMessage}
                onChange={(val) =>
                  setConfig((prev) => ({
                    ...prev,
                    Gpt: { ...prev?.Log, Organization: val },
                  }))
                }
              />
            </ServerSettingBox>
          </div>
        </div>
        <div className="flex gap-2 self-end p-2">
          <button
            type="button"
            className="base-bg-gradient-r rounded-lg p-3 text-xl font-bold duration-150 hover:scale-105"
            onClick={resetToDefaults}
          >
            Reset Changes
          </button>
          <button
            type="button"
            className="base-bg-gradient-r rounded-lg p-3 text-xl font-bold duration-150 hover:scale-105"
            onClick={updateServer}
          >
            Update Server
          </button>
        </div>
      </div>
    </div>
  );
};

type ServerSettingBoxProps = {
  name: string;
  children: string | JSX.Element | JSX.Element[];
};

const ServerSettingBox = ({ name, children }: ServerSettingBoxProps) => {
  return (
    <div className="relative flex flex-[40%] flex-col gap-2 rounded bg-backgroundColor p-2 pt-8">
      <div className="absolute -top-4 left-10 rounded bg-element p-2">
        {name}
      </div>
      {children}
    </div>
  );
};

type ServerSettingInputProps = {
  currValue: string | number | undefined | null;
  label: string;
  note?: string;
  onChange: (value: string) => void;
};

const ServerSettingInput = ({
  currValue,
  label,
  note,
  onChange,
}: ServerSettingInputProps) => {
  return (
    <>
      <label>{label}</label>
      <input
        type="text"
        className="base-input w-full"
        defaultValue={currValue ?? ""}
        onChange={(val) => onChange(val.target.value)}
      />
    </>
  );
};
type NumericServerSettingInputProps = {
  currValue: string | number | undefined | null;
  label: string;
  note?: string;
  min?: number;
  max?: number;
  onChange: (value: number) => void;
};

const NumericServerSettingInput = ({
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

type ServerCheckboxInputProps = {
  currValue: boolean | null | undefined;
  label: string;
  note?: string;
  onChange: (value: boolean) => void;
};

const ServerCheckboxInput = ({
  currValue,
  label,
  note,
  onChange,
}: ServerCheckboxInputProps) => {
  return (
    <>
      <label>{label}</label>
      <input
        id="default-checkbox"
        type="checkbox"
        defaultChecked={currValue ?? false}
        onChange={(val) => onChange(val.target.checked)}
        className="text-blue-600 bg-gray-100 border-gray-300 focus:ring-blue-500 dark:focus:ring-blue-600 dark:ring-offset-gray-800 dark:bg-gray-700 dark:border-gray-600 h-4 w-4 rounded focus:ring-2"
      />
    </>
  );
};

export default ServerPage;
