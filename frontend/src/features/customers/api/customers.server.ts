import "server-only";
import { backendResponse } from "@/services/api/api-client.server";

export const customersServerApi = {
  contracts: (id: string, search: string, token: string, signal?: AbortSignal) =>
    backendResponse(`/api/customers/${encodeURIComponent(id)}/contracts${search}`, token, {
      signal,
    }),
  suggestions: (search: string, token: string, signal?: AbortSignal) =>
    backendResponse(`/api/customers/suggestions${search}`, token, { signal }),
  list: (search: string, token: string, signal?: AbortSignal) =>
    backendResponse(`/api/customers${search}`, token, { signal }),
  movements: (id: string, search: string, token: string, signal?: AbortSignal) =>
    backendResponse(`/api/customers/${encodeURIComponent(id)}/movements${search}`, token, {
      signal,
    }),
};
