import { lazy } from "react";
export { MorphAvatar } from "./MorphAvatar";
export { TextOutput } from "./TextOutput";
export { CommandOutputRenderer } from "./CommandOutputRenderer";

export const GraphOutput = lazy(() =>
  import("./GraphOutput").then(({ GraphOutput }) => ({
    default: GraphOutput,
  }))
);
