export interface ApiOptions {
  params?: Record<string, string | number | boolean | undefined>;
  headers?: Record<string, string>;
  signal?: AbortSignal;
}

export function withParams(path: string, params: ApiOptions["params"]): string {
  const query = new URLSearchParams();
  for (const [key, value] of Object.entries(params ?? {}))
    if (value !== undefined) query.set(key, String(value));
  return query.size ? `${path}${path.includes("?") ? "&" : "?"}${query}` : path;
}
