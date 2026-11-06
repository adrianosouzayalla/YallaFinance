import "server-only";
import { backendResponse } from "@/services/api/apiClient.server";

export const publicPaymentServerApi = {
  checkout: (token: string) => backendResponse(`/api/payments/${encodeURIComponent(token)}`),
};
