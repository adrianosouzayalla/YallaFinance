import "server-only";
import { backendResponse } from "@/services/api/apiClient.server";

export const debtsServerApi = {
  createManualDebt: (body: string, token: string, signal?: AbortSignal) =>
    backendResponse("/api/debts", token, {
      method: "POST",
      body,
      headers: { "Content-Type": "application/json" },
      signal,
    }),
  list: (search: string, token: string, signal?: AbortSignal) =>
    backendResponse(`/api/debts${search}`, token, { signal }),
  detail: (id: string, token: string, signal?: AbortSignal) =>
    backendResponse(`/api/debts/${encodeURIComponent(id)}`, token, { signal }),
};
