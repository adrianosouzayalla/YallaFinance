export const SESSION_CONTEXT_HEADER = "X-Yalla-Context";
export interface SessionApiOptions {
  contextId: string;
  signal?: AbortSignal;
}
export function sessionRequestOptions(options: SessionApiOptions): {
  headers: Record<string, string>;
  signal?: AbortSignal;
} {
  return { headers: { [SESSION_CONTEXT_HEADER]: options.contextId }, signal: options.signal };
}
