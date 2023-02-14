import { CommandResponse } from "../../api";

export interface UserCommand {
  prompt?: string;
  response?: CommandResponse | undefined;
  isLoading: boolean;
}
