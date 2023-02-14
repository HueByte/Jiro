import { lazy } from "react";

export { TextOutput } from "./TextOutput";
export { CommandOutputRenderer } from "./CommandOutputRenderer";

export const GraphOutput = lazy(() =>
  import("./GraphOutput").then(({ GraphOutput }) => ({
    default: GraphOutput,
  }))
);
