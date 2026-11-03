import "server-only";
import type { Session } from "../authSession";
export interface ServerSession extends Session { token: string; expiresAt: string; }

export function assertCookieToken(token: string): void {
  if (!/^[A-Za-z0-9_-]{43}$/.test(token) || Buffer.from(token, "base64url").toString("base64url") !== token)
    throw new Error("Token de sessão inválido.");
}

// Metadata comes exclusively from the API's validated database session.
export function parseSessionProfile(token: string, profile: unknown): ServerSession | null {
  try { assertCookieToken(token); } catch { return null; }
  if (!profile || typeof profile !== "object") return null;
  const value = profile as Record<string, unknown>;
  if (value.authenticated !== true || typeof value.displayName !== "string"
    || typeof value.tenantId !== "string" || !value.tenantId
    || typeof value.contextId !== "string" || !value.contextId
    || typeof value.expiresAt !== "string" || !Number.isFinite(Date.parse(value.expiresAt))
    || Date.parse(value.expiresAt) <= Date.now()) return null;
  return {
    token, userId: typeof value.userId === "string" ? value.userId : undefined,
    displayName: value.displayName, tenantId: value.tenantId, contextId: value.contextId,
    expiresAt: value.expiresAt, organization: typeof value.organization === "string" ? value.organization : undefined,
    functionalityCodes: Array.isArray(value.functionalityCodes)
      ? [...new Set(value.functionalityCodes.filter((code): code is string => typeof code === "string").map((code) => code.toUpperCase()))]
      : [],
  };
}
