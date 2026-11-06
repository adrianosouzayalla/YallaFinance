import "server-only";
import { backendResponse } from "@/services/api/apiClient.server";

export const chargesServerApi = {
  directChargeStatus: (id: string, token: string, signal?: AbortSignal) =>
    backendResponse(`/api/charges/direct/${encodeURIComponent(id)}/status`, token, { signal }),
  submitDirectCharge: (
    body: string,
    idempotencyKey: string | null,
    token: string,
    signal?: AbortSignal,
  ) =>
    backendResponse("/api/charges/direct", token, {
      method: "POST",
      body,
      headers: idempotencyKey ? { "Idempotency-Key": idempotencyKey } : {},
      signal,
    }),
  detail: (id: string, token: string, signal?: AbortSignal) =>
    backendResponse(`/api/charges/${encodeURIComponent(id)}`, token, { signal }),
  create: (body: string, idempotencyKey: string | null, token: string, signal?: AbortSignal) =>
    backendResponse("/api/charges", token, {
      method: "POST",
      body,
      headers: idempotencyKey ? { "Idempotency-Key": idempotencyKey } : {},
      signal,
    }),
};
