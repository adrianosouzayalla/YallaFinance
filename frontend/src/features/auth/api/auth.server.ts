import "server-only";
import { backendResponse, serverApiClient } from "@/services/api/api-client.server";
import type { LoginInput, TenantOption } from "../authSession";

export interface ApiLoginResult {
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
};
