import "server-only";
import { backendResponse } from "@/services/api/apiClient.server";
export const jobsServerApi = {
  async reprocess(id: string, token: string, signal?: AbortSignal) {
    const response = await backendResponse(`/api/jobs/${encodeURIComponent(id)}/reprocess`, token, {
      method: "POST",
      signal,
    });
    if (response.status === 409) {
      const notification = await response
        .clone()
        .json()
        .catch(() => null);
      const conflict = Array.isArray(notification?.errors)
        ? notification.errors.find(
            (error: { code?: string; message?: string }) =>
              error?.code === "Conflict" && typeof error.message === "string",
          )
        : null;
      if (conflict)
        return Response.json(
          { code: conflict.code, detail: conflict.message },
          {
            status: 409,
            headers: { "Cache-Control": "private, no-store" },
          },
        );
    }
    return response;
  },
  types(token: string, signal?: AbortSignal) {
    return backendResponse("/api/jobs/types", token, { signal });
  },
  list(search: string, token: string, signal?: AbortSignal) {
    return backendResponse(`/api/jobs${search}`, token, { signal });
  },
  detail(id: string, token: string, signal?: AbortSignal) {
    return backendResponse(`/api/jobs/${encodeURIComponent(id)}`, token, { signal });
  },
  executions(id: string, search: string, token: string, signal?: AbortSignal) {
    return backendResponse(`/api/jobs/${encodeURIComponent(id)}/executions${search}`, token, {
      signal,
    });
  },
};
