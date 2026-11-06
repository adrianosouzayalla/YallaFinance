import "server-only";
import { backendResponse, serverApiClient } from "@/services/api/apiClient.server";
import type { LoginInput, TenantOption } from "@/services/api/auth/authSession";

interface ApiLoginResult {
  success: boolean;
  token: string | null;
  displayName: string | null;
  errorCode?: string;
  expiresAt?: string;
  tenantId?: string;
  tenants?: TenantOption[];
}
export const authServerApi = {
  login: (input: LoginInput) => serverApiClient().post<ApiLoginResult>("/api/auth/login", input),
  session: (token: string, signal?: AbortSignal) =>
    backendResponse("/api/auth/session", token, { signal }),
  logout: (token: string) => backendResponse("/api/auth/logout", token, { method: "POST" }),
  // Anonymous, bound to the ticket issued by a login that requires the password change (feature 28, phase 3).
  changeRequiredPassword: (input: ChangeRequiredPasswordInput) =>
    backendResponse("/api/auth/password-change-required", undefined, {
      method: "POST",
      body: JSON.stringify(input),
      headers: { "Content-Type": "application/json" },
    }),
};

interface ChangeRequiredPasswordInput {
  email: string;
  ticket: string;
  currentPassword: string;
  newPassword: string;
  confirmation: string;
}
