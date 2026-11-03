// @vitest-environment node
import { beforeEach, expect, it, vi } from "vitest";
import { discardLocalSessionAction, loginAction, logoutAction } from "./actions";
import { authServerApi } from "./api/auth.server";
import { ApiError } from "@/services/api/api-error";
import { createSession, deleteSession, readSession } from "./server/session";

vi.mock("server-only", () => ({}));
vi.mock("next/navigation", () => ({ redirect: vi.fn(() => { throw new Error("REDIRECT"); }) }));
vi.mock("./api/auth.server", () => ({ authServerApi: { login: vi.fn(), logout: vi.fn() } }));
vi.mock("./server/session", () => ({ createSession: vi.fn(), deleteSession: vi.fn(), readSession: vi.fn() }));

beforeEach(() => vi.resetAllMocks());

it("keeps the cookie when backend revocation fails, so the user can retry", async () => {
  vi.mocked(readSession).mockResolvedValue({ token: "private", displayName: "Admin", expiresAt: "2099-01-01" });
  vi.mocked(authServerApi.logout).mockResolvedValue(new Response(null, { status: 503 }));
  expect((await logoutAction()).error).toMatch(/Tente novamente/);
  expect(deleteSession).not.toHaveBeenCalled();
});

it("discards the local session cookie without calling an unavailable backend", async () => {
  await discardLocalSessionAction();
  expect(deleteSession).toHaveBeenCalledOnce();
  expect(authServerApi.logout).not.toHaveBeenCalled();
});

it("does not invent expiration and revokes a token if its login response cannot create a usable cookie", async () => {
  vi.mocked(authServerApi.login).mockResolvedValue({ success: true, token: "private", displayName: "Admin" });
  vi.mocked(authServerApi.logout).mockResolvedValue(new Response(null, { status: 200 }));
  const form = new FormData(); form.set("email", "admin@yalla.com.br"); form.set("password", "demo");
  const result = await loginAction({ fieldErrors: {}, formError: "" }, form);
  expect(result.formError).toMatch(/sem validade/);
  expect(createSession).not.toHaveBeenCalled();
  expect(authServerApi.logout).toHaveBeenCalledWith("private");
});

it("tells the user about too many attempts when the backend blocks the login with 429", async () => {
  vi.mocked(authServerApi.login).mockRejectedValue(new ApiError("API_ERROR", "Falha na API", 429));
  const form = new FormData(); form.set("email", "admin@yalla.com.br"); form.set("password", "errada");
  const result = await loginAction({ fieldErrors: {}, formError: "" }, form);
  expect(result.formError).toMatch(/Muitas tentativas de login/);
  expect(createSession).not.toHaveBeenCalled();
});

it("keeps the generic message for other login failures", async () => {
  vi.mocked(authServerApi.login).mockRejectedValue(new ApiError("API_ERROR", "Falha na API", 500));
  const form = new FormData(); form.set("email", "admin@yalla.com.br"); form.set("password", "errada");
  const result = await loginAction({ fieldErrors: {}, formError: "" }, form);
  expect(result.formError).toMatch(/Não foi possível iniciar a sessão/);
});
