import "server-only";
import { backendResponse } from "@/services/api/api-client.server";

export function paymentBackend(path: string, token: string, init: RequestInit = {}) {
  return backendResponse(`/api/payments/${path}`, token, init);
}
