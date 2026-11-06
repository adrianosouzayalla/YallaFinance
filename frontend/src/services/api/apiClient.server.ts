import "server-only";
import axios from "axios";
import { ApiError } from "./apiError";
import { withParams, type ApiOptions } from "./apiContract";

export async function backendResponse(
  path: string,
  token?: string,
  init: RequestInit = {},
): Promise<Response> {
  const baseURL = process.env.API_URL?.trim();
  if (!baseURL) throw new Error("API_URL must be configured on the Next server.");
  if (!path.startsWith("/api/") || path.startsWith("//") || path.includes("\\"))
    throw new Error("Invalid backend path");
  const incoming = new Headers(init.headers);
  const response = await axios.request<string>({
    baseURL,
    url: path,
    method: init.method ?? "GET",
    data: init.body,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(incoming.get("Idempotency-Key")
        ? { "Idempotency-Key": incoming.get("Idempotency-Key")! }
        : {}),
    },
    signal: init.signal ?? undefined,
    maxRedirects: 0,
    responseType: "text",
    transformResponse: [],
    validateStatus: () => true,
  });
  return new Response(response.status === 204 || response.status === 304 ? null : response.data, {
    status: response.status,
    headers: {
      "Content-Type": String(response.headers["content-type"] ?? "application/json"),
      "Cache-Control": "private, no-store",
    },
  });
}

async function body<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const problem = await response.json().catch(() => ({}));
    throw new ApiError(
      problem.extensions?.code ?? problem.code ?? "API_ERROR",
      problem.detail ?? "Falha na API",
      response.status,
    );
  }
  return response.status === 204 ? (undefined as T) : (response.json() as Promise<T>);
}

export function serverApiClient(token?: string) {
  return {
    get: async <T>(path: string, options: ApiOptions = {}) =>
      body<T>(
        await backendResponse(withParams(path, options.params), token, {
          headers: options.headers,
          signal: options.signal,
        }),
      ),
    post: async <T>(path: string, data?: unknown, options: ApiOptions = {}) =>
      body<T>(
        await backendResponse(withParams(path, options.params), token, {
          method: "POST",
          body: data === undefined ? undefined : JSON.stringify(data),
          headers: options.headers,
          signal: options.signal,
        }),
      ),
  };
}
