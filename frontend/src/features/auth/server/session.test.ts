// @vitest-environment node
import { beforeEach, expect, it, vi } from "vitest";
import { readSession, createSession } from "./session";
import { authServerApi } from "../api/auth.server";
const cookie = vi.hoisted(() => ({ get: vi.fn(), set: vi.fn(), delete: vi.fn() }));
vi.mock("server-only", () => ({}));
vi.mock("next/headers", () => ({ cookies: async () => cookie }));
vi.mock("../api/auth.server", () => ({ authServerApi: { session: vi.fn() } }));
const token = Buffer.alloc(32, 7).toString("base64url");
const profile = { authenticated: true, displayName: "Administrador", tenantId: "tenant-a",
  contextId: "session-a", organization: "Yalla", expiresAt: "2099-01-01T00:00:00.000Z" };
beforeEach(() => { vi.resetAllMocks(); cookie.get.mockReturnValue({ value: token }); });
it("loads presentation context from the API instead of decoding the bearer", async () => {
  vi.mocked(authServerApi.session).mockResolvedValue(Response.json(profile));
  expect(await readSession()).toEqual(expect.objectContaining({ token, tenantId: "tenant-a", contextId: "session-a" }));
  expect(authServerApi.session).toHaveBeenCalledWith(token);
});
it("does not treat an API failure as logout or delete the cookie", async () => {
  vi.mocked(authServerApi.session).mockResolvedValue(new Response(null, { status: 503 }));
  await expect(readSession()).rejects.toThrow();
  expect(cookie.delete).not.toHaveBeenCalled();
});
it("writes only the opaque bearer to the HttpOnly cookie", async () => {
  await createSession({ token, displayName: "Admin", expiresAt: profile.expiresAt });
  expect(cookie.set).toHaveBeenCalledWith("yalla-finance-session", token,
    expect.objectContaining({ httpOnly: true, sameSite: "lax", path: "/", expires: new Date(profile.expiresAt) }));
});
it("rejects invalid and legacy cookies without asking the API to decode them", async () => {
  cookie.get.mockReturnValue({ value: "header.payload.signature" });
  expect(await readSession()).toBeNull();
  expect(authServerApi.session).not.toHaveBeenCalled();
});
it("uses the API's 401 as the source of session invalidation", async () => {
  vi.mocked(authServerApi.session).mockResolvedValue(new Response(null, { status: 401 }));
  expect(await readSession()).toBeNull();
});
