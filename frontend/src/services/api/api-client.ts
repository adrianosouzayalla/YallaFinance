import axios from "axios";
import { ApiError } from "./api-error";
import { withParams, type ApiOptions } from "./api-contract";

export { ApiError } from "./api-error";

export async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  let response;
  try {
    response = await axios.request<string>({
      url: path,
      method: init.method ?? "GET",
      data: init.body,
      headers: {
        "Content-Type": "application/json",
        ...Object.fromEntries(new Headers(init.headers).entries()),
      },
      signal: init.signal ?? undefined,
      responseType: "text",
      transformResponse: [],
      validateStatus: () => true,
    });
  } catch (error) {
    if (
      typeof window !== "undefined" &&
      axios.isAxiosError(error) &&
      !error.response &&
      !axios.isCancel(error)
    ) {
      window.dispatchEvent(new Event("yalla:api-unavailable"));
    }
    throw error;
  }
  if (response.status < 200 || response.status >= 300) {
    let problem: {
      detail?: string;
      code?: string;
      extensions?: { code?: string };
      errors?: Array<{ code?: string; message?: string }>;
    } = {};
    try {
      problem = JSON.parse(response.data);
    } catch {
      /* Non-JSON failure response. */
    }
    const firstError = problem.errors?.[0];
    const contextChanged = response.status === 409 && problem.code === "SESSION_CONTEXT_CHANGED";
    const errorCode = problem.extensions?.code ?? problem.code ?? firstError?.code;
    if (errorCode === "API_UNAVAILABLE" && typeof window !== "undefined") {
      window.dispatchEvent(new Event("yalla:api-unavailable"));
    }
    if (
      (response.status === 401 || contextChanged) &&
      typeof window !== "undefined" &&
      !path.startsWith("/api/auth/")
    ) {
      window.dispatchEvent(
        new CustomEvent("yalla:auth-failure", { detail: { status: response.status } }),
      );
    }
    throw new ApiError(
      errorCode ?? "API_ERROR",
      problem.detail ?? firstError?.message ?? "Falha na API",
      response.status,
    );
  }
  if (response.status === 204) return undefined as T;
  const responseContentType = response.headers?.["content-type"];
  if (typeof responseContentType === "string" && !responseContentType.toLowerCase().includes("json"))
    return response.data as T;
  return JSON.parse(response.data) as T;
}

export const apiClient = {
  get: <T>(path: string, options: ApiOptions = {}) =>
    request<T>(withParams(path, options.params), {
      headers: options.headers,
      signal: options.signal,
    }),
  post: <T>(path: string, body?: unknown, options: ApiOptions = {}) =>
    request<T>(withParams(path, options.params), {
      method: "POST",
      body: body === undefined ? undefined : JSON.stringify(body),
      headers: options.headers,
      signal: options.signal,
    }),
};
