import { sessionRequestOptions, type SessionApiOptions } from "@/services/api/sessionOptions";
import { apiClient } from "@/services/api/apiClient";
import { directoryApi } from "@/services/api/directoryApi";
export const authClientApi = {
  session: (options: SessionApiOptions) =>
    apiClient.get<{ authenticated: boolean; displayName: string; expiresAt?: string }>(
      "/api/auth/session",
      sessionRequestOptions(options),
    ),
  changePassword: (input: { currentPassword: string; newPassword: string }, contextId: string) =>
    directoryApi.send("/api/auth/password", "POST", input, contextId),
};
