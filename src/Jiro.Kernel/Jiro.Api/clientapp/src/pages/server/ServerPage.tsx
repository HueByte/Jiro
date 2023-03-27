import { ChangeEvent, useEffect, useState } from "react";
import {
  AiFillEye,
  AiFillEyeInvisible,
  AiOutlineBlock,
  AiOutlineBug,
} from "react-icons/ai";
import { InstanceConfigDTO, ServerService } from "../../api";
import { promiseToast, updatePromiseToast } from "../../lib";
import { infoToast } from "../../lib/notifications";
import { TbShieldLock } from "react-icons/tb";
import { BsFillCloudHazeFill } from "react-icons/bs";
import Loader from "../../components/Loader";

const ServerPage = () => {
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [initialValue, setInitialValue] = useState<InstanceConfigDTO | null>(
    null
  );
  const [config, setConfig] = useState<InstanceConfigDTO | null>(null);

  useEffect(() => {
    (async () => {
      let id = promiseToast("Loading server settings...");
      try {
        let config = await ServerService.getApiServer();
        if (config.isSuccess && config.data) {
          updatePromiseToast(id, "Server settings loaded", "success", 2000);
          setInitialValue(config.data);
          setConfig(config.data);
        } else {
          updatePromiseToast(id, "Server settings failed to load", "error");
        }
      } catch (err: any) {
        updatePromiseToast(id, "Server settings failed to load", "error");
      }

      setIsLoading(false);
    })();
  }, []);

  const updateServer = async () => {
    if (config) {
      let promiseId = promiseToast("Updating server settings...");

      try {
        let result = await ServerService.putApiServer({ requestBody: config });
        if (result.isSuccess) {
          setInitialValue(config);

          updatePromiseToast(
            promiseId,
            "Server settings updated successfully",
            "success"
          );

          infoToast("🦄 Restarting server...");
        } else {
          updatePromiseToast(
            promiseId,
            result.errors?.join(", ") ?? "Uknown error occured",
            "error"
          );
        }
      } catch (err: any) {
        updatePromiseToast(
          promiseId,
          err.body?.errors?.join(", ") ?? "Uknown error occured",
          "error"
        );
      }
    }
  };

  const resetToDefaults = () => {
    infoToast("Server settings reset to defaults");
    setConfig(initialValue);
  };

  return (
    <div className="flex h-full w-full justify-center px-8 py-8 lg:px-0">
      <div className="flex h-full w-5/6 max-w-[1024px] flex-col rounded-xl bg-element p-2 pt-6 shadow-lg shadow-element lg:w-11/12">
        {!isLoading ? (
          <>
            <div className="flex-1 overflow-y-auto p-2">
              <div className="flex w-full flex-row flex-wrap gap-4 text-lg">
                <ServerSettingBox
                  icon={<AiOutlineBlock className="inline" />}
                  name="Main Settings"
                >
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

                <ServerSettingBox
                  icon={<TbShieldLock className="inline" />}
                  name="Authentication Settings"
                >
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
                    isSecret={true}
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

                <ServerSettingBox
                  icon={<AiOutlineBug className="inline" />}
                  name="Logger Settings"
                >
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

                <ServerSettingBox
                  icon={<BsFillCloudHazeFill className="inline" />}
                  name="GPT"
                >
                  <ServerCheckboxInput
                    label="Enable GPT Chat"
                    currValue={config?.Gpt?.Enable}
                    onChange={(val) =>
                      setConfig((prev) => ({
                        ...prev,
                        Gpt: { ...prev?.Gpt, Enable: val },
                      }))
                    }
                  />
                  <ServerSettingInput
                    label="OpenAI Url"
                    currValue={config?.Gpt?.BaseUrl}
                    onChange={(val) =>
                      setConfig((prev) => ({
                        ...prev,
                        Gpt: { ...prev?.Gpt, BaseUrl: val },
                      }))
                    }
                  />
                  <ServerSettingInput
                    label="OpenAI Authentication Token"
                    currValue={config?.Gpt?.AuthToken}
                    isSecret={true}
                    onChange={(val) =>
                      setConfig((prev) => ({
                        ...prev,
                        Gpt: { ...prev?.Gpt, AuthToken: val },
                      }))
                    }
                  />
                  <ServerSettingInput
                    label="OpenAI Organization"
                    currValue={config?.Gpt?.Organization}
                    onChange={(val) =>
                      setConfig((prev) => ({
                        ...prev,
                        Gpt: { ...prev?.Gpt, Organization: val },
                      }))
                    }
                  />
                  <ServerTextAreaInput
                    label="ChatGPT System Message"
                    currValue={config?.Gpt?.ChatGpt?.SystemMessage ?? ""}
                    onChange={(val) =>
                      setConfig((prev) => ({
                        ...prev,
                        Gpt: {
                          ...prev?.Gpt,
                          ChatGpt: {
                            ...prev?.Gpt?.ChatGpt,
                            SystemMessage: val,
                          },
                        },
                      }))
                    }
                  />
                </ServerSettingBox>
              </div>
            </div>
            <div className="flex gap-2 self-end p-2 md:w-full md:flex-col">
              <button
                type="button"
                className="rounded-lg bg-elementLight p-3 text-xl font-bold duration-150 hover:scale-105"
                onClick={resetToDefaults}
              >
                Reset Changes
              </button>
              <button
                type="button"
                className="rounded-lg bg-elementLight p-3 text-xl font-bold duration-150 hover:scale-105"
                onClick={updateServer}
              >
                Update Server
              </button>
            </div>
          </>
        ) : (
          <Loader />
        )}
      </div>
    </div>
  );
};

type ServerSettingBoxProps = {
  icon: JSX.Element;
  name: string;
  children: string | JSX.Element | JSX.Element[];
};

const ServerSettingBox = ({ icon, name, children }: ServerSettingBoxProps) => {
  return (
    <div className="relative flex flex-[40%] flex-col gap-2 rounded bg-backgroundColor p-2 pt-8 md:flex-[100%]">
      <div className="absolute -top-4 left-10 rounded bg-element px-2 py-1 font-bold text-accent7">
        {icon} {name}
      </div>
      {children}
    </div>
  );
};

type ServerSettingInputProps = {
  currValue: string | number | undefined | null;
  label: string;
  note?: string;
  isSecret?: boolean;
  onChange: (value: string) => void;
};

const ServerSettingInput = ({
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

type ServerTextAreaInputProps = {
  currValue: string | number | readonly string[];
  label: string;
  note?: string;
  onChange: (value: string) => void;
};

const ServerTextAreaInput = ({
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

export default ServerPage;
