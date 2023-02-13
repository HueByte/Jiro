import { ICommandResultCommandResponse } from "../../api";

export interface UserCommand {
  prompt?: string;
  response?: any | undefined;
  isLoading: boolean;
}
