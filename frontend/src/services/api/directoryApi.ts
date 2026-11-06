import { request } from "@/services/api/apiClient";
import { sessionRequestOptions } from "@/services/api/sessionOptions";

export const directoryApi = {
  get<T>(path: string, contextId: string, signal?: AbortSignal): Promise<T> {
    return request<T>(path, sessionRequestOptions({ contextId, signal }));
  },
  send<T = void>(
    path: string,
    method: "POST" | "PUT",
    data: unknown,
    contextId: string,
  ): Promise<T> {
    return request<T>(path, {
      ...sessionRequestOptions({ contextId }),
      method,
      body: JSON.stringify(data),
    });
  },
};

export function directoryQuery(filters: object): string {
  const values = Object.entries(filters).filter(
    ([, value]) => value !== "" && value !== undefined && value !== null,
  );
  return new URLSearchParams(values.map(([key, value]) => [key, String(value)])).toString();
}

export function directoryError(error: unknown): string {
  return error instanceof Error ? error.message : "Não foi possível concluir. Tente novamente.";
}
