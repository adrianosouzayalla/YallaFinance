import "server-only";
import { backendResponse } from "@/services/api/apiClient.server";

export const dashboardServerApi = {
  summary: (token: string, signal?: AbortSignal) =>
    backendResponse("/api/dashboard/summary", token, { signal }),
};
