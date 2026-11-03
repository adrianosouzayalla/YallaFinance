import { sessionRequestOptions, type SessionApiOptions } from "@/features/auth/api/session-options";
import { apiClient } from "@/services/api/api-client";
export const authClientApi = {
  session: (options: SessionApiOptions) =>
    apiClient.get<{ authenticated: boolean; displayName: string; expiresAt?: string }>(
      "/api/auth/session",
      sessionRequestOptions(options),
    ),
};
