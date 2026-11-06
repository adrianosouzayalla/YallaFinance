import "server-only";
import { cookies } from "next/headers";
import { SESSION_COOKIE } from "@/services/api/auth/authSession";
import { authServerApi } from "@/services/api/auth/authApi.server";
import { assertCookieToken, parseSessionProfile, type ServerSession } from "./sessionCookie";

export async function readSession(): Promise<ServerSession | null> {
  const token = (await cookies()).get(SESSION_COOKIE)?.value;
  if (!token) return null;
  try {
    assertCookieToken(token);
  } catch {
    return null;
  }
  const response = await authServerApi.session(token);
  if (response.status === 401) return null;
  if (!response.ok) throw new Error("Não foi possível consultar a sessão. Tente novamente.");
  const profile = parseSessionProfile(token, await response.json());
  if (!profile) throw new Error("A API retornou dados de sessão inválidos.");
  return profile;
}
export async function deleteSession(): Promise<void> {
  (await cookies()).delete(SESSION_COOKIE);
}
export async function createSession(session: ServerSession): Promise<void> {
  assertCookieToken(session.token);
  const expires = new Date(session.expiresAt);
  if (!Number.isFinite(expires.getTime()) || expires.getTime() <= Date.now())
    throw new Error("Sessão sem validade.");
  (await cookies()).set(SESSION_COOKIE, session.token, {
    httpOnly: true,
    sameSite: "lax",
    path: "/",
    secure: process.env.NODE_ENV === "production",
    expires,
  });
}
