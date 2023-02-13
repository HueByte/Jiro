import { CommandResponse } from "../../api";

export interface UserCommand {
  prompt?: string;
  response?: CommandResponse;
  isLoading: boolean;
}
